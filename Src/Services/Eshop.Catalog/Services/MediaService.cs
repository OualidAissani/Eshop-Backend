using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Polly;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Eshop.Catalog.Services;

public class MediaService : IMediaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string MediaBaseUrl = "https://upload.uploadcare.com/base/";
    private const string Deleteurl = $"https://api.uploadcare.com/files/storage/";
    private readonly IConfiguration _configuration;
    private readonly ILogger<MediaService> _logger;
    public MediaService(IHttpClientFactory httpClietnt, IConfiguration configuration, ILogger<MediaService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClietnt;
        _logger = logger;
    }
    public async Task<ProductMediaItem> CreateMedia(ProductMediaItem media, Stream fileStream,
    string contentType, string fileName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(media);
        ArgumentNullException.ThrowIfNull(fileStream);

        // Buffer entirely — makes Polly retries replayable
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, ct);
        var fileBytes = ms.ToArray();

        var httpClient = _httpClientFactory.CreateClient();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(_configuration["UploadCare:PublicKey"]), "UPLOADCARE_PUB_KEY");
        content.Add(new StringContent(_configuration["UploadCare:Store"]), "UPLOADCARE_STORE");

        if (string.IsNullOrWhiteSpace(_configuration["UploadCare:PublicKey"]))
            throw new InvalidOperationException("UploadCare:PublicKey is not configured.");


        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        var response = await httpClient.PostAsync(MediaBaseUrl, content, ct);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: ct);
        var uuid = doc.RootElement.GetProperty("file").GetString();

        media.Media = _configuration["UploadCare:UploadCareBaseUrl"] + uuid + $"/{fileName}";
        return media;
    }


    public async Task<bool> DeleteMedia(string mediaUrl,CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            return false;
        }
        var publicKey = _configuration["UploadCare:PublicKey"];
        var secretKey = _configuration["UploadCare:SecretKey"];
        var httpClient = _httpClientFactory.CreateClient();

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Uploadcare.Simple", $"{publicKey}:{secretKey}");
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.uploadcare-v0.7+json"));

        var uuid = mediaUrl.Split('/');
        var jsonContent = new StringContent(JsonSerializer.Serialize(new[] { uuid[^2] }),System.Text.Encoding.UTF8,"application/json");


        var request = new HttpRequestMessage(HttpMethod.Delete, Deleteurl)
        {
            Content = jsonContent
        };

        var response = await httpClient.SendAsync(request);

        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        return true;
    }

    public async Task<List<ProductMediaItem>> ProductMedias(ProductCreateDto product, List<IFormFile> formFile, CancellationToken ct)
    {
        var mediaItems = new List<ProductMediaItem>();
        foreach (var file in formFile)
        {
            using var stream = file.OpenReadStream();
            var media = new ProductMediaItem
            {
                Description = product.Description
            };

            var createdMedia = await CreateMedia(media, stream, file.ContentType ?? "application/octet-stream", file.FileName, ct);
            mediaItems.Add(new ProductMediaItem
            {
                Media = createdMedia.Media,
                Description = createdMedia.Description
            });
        }

        return mediaItems;
    }

    public async Task DeleteOldProductMedia(ProductDocument product, CancellationToken ct)
    {
        var mediaRetryPolicy = Policy
            .Handle<InvalidOperationException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning($"Media deletion failed. Retry {retryCount}/3 after {timespan.TotalSeconds}s. Error: {exception.Message}");
                });

        await mediaRetryPolicy.ExecuteAsync(async () =>
        {
            var deletionResult = await Task.WhenAll(product.Media.Select(m => DeleteMedia(m.Media, ct)));

            if (!deletionResult.All(r => r))
            {
                throw new InvalidOperationException("The Media Deletion Process Failed");
            }
        });
    }
}

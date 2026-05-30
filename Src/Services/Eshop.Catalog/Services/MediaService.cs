using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Microsoft.AspNetCore.Mvc;
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
    public MediaService(IHttpClientFactory httpClietnt,IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClientFactory = httpClietnt;
    }
    public async Task<ProductMedia> CreateMedia(ProductMedia media, Stream fileStream, string contentType, string fileName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(media);
        ArgumentNullException.ThrowIfNull(fileStream);

        var httpClient = _httpClientFactory.CreateClient();

        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(_configuration["UploadCare:PublicKey"]), "UPLOADCARE_PUB_KEY");

        content.Add(new StringContent(_configuration["UploadCare:Store"]), "UPLOADCARE_STORE");

        var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        content.Add(fileContent, "file", fileName);

        var response = await httpClient.PostAsync(MediaBaseUrl, content);

        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);

        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: ct);
        var uuid = doc.RootElement.GetProperty("file").GetString();

        media.Media = _configuration["UploadCare:UploadCareBaseUrl"] + uuid + $"/{fileName}";
        

        return media;
    }


    public async Task<bool> DeleteMedia(string uuid,CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(uuid))
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


        var jsonContent = new StringContent(JsonSerializer.Serialize(new[] { uuid }),System.Text.Encoding.UTF8,"application/json");


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
}

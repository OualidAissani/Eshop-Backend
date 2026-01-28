using Eshop.Catalog.Data;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Eshop.Catalog.Services
{
    public class MediaService : IMediaService
    {
        private readonly CatalogDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string MediaBaseUrl = "https://upload.uploadcare.com/base/";
        private readonly IConfiguration _configuration;
        public MediaService(CatalogDbContext context, IHttpClientFactory httpClietnt,IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClietnt;
        }
        public async Task<ProductMedia> CreateMedia(ProductMedia media, Stream fileStream,string contentType ,string fileName)
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(_configuration["UploadCare:PublicKey"]), "UPLOADCARE_PUB_KEY");
            content.Add(new StringContent(_configuration["UploadCare:Store"]), "UPLOADCARE_STORE");
            //31a2bxztr3.ucarecd.net (+ uuid for acual image)
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var response = await httpClient.PostAsync(MediaBaseUrl, content);
            response.EnsureSuccessStatusCode();
            var contentresponse=await response.Content.ReadAsStringAsync();

            var jsonResponse = System.Text.Json.JsonDocument.Parse(contentresponse);
            var uuid = jsonResponse.RootElement.GetProperty("file").GetString();

            media.Media = uuid; _context.Media.Add(media);
            await  _context.SaveChangesAsync();

            return media;
        }

        public async Task<bool> DeleteMedia(string uuid)
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

            var url = $"https://api.uploadcare.com/files/storage/";

            var jsonContent = new StringContent(
        System.Text.Json.JsonSerializer.Serialize(new[] { uuid }),
        System.Text.Encoding.UTF8,
        "application/json");


            var request = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = jsonContent
            };

            var response = await httpClient.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }
    }
}

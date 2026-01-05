namespace Eshop.Web.Services
{
    public class CatalogService
    {
        private readonly HttpClient _httpClient;
        private string BaseUrl = "https://localhost:7194";
        public CatalogService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task GetAllProductsPaginated()
        {
            var products=await _httpClient.GetStringAsync($"{BaseUrl}/api/catalog/GetProducts");
        }
    }
}

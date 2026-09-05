using Eshop.Orders.Services.IServices;
using System.Text.Json;

namespace Eshop.Orders.Services
{
    public class EmailService : IEmailService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public EmailService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct )
        {
            var client = _httpClientFactory.CreateClient("EmailService");

            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("api-key", _configuration["EmailService:ApiKey"]);
            var url = $"{_configuration["EmailService:BaseUrl"]}smtp/email";

            var sender = new
            {
                name = _configuration["EmailService:SenderName"],
                email = _configuration["EmailService:SenderEmail"]
            };
            var toAddress = new[]
            {
                new {email = toEmail }
            };
            var request=new 
            {
                sender,
                to= toAddress,
                subject,
                textContent=body,
            };
            var jsoncontent = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
            var message = new HttpRequestMessage(HttpMethod.Post, url)
            {
            Content= jsoncontent
            };
            var response=await client.SendAsync(message);

            if(response.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }
    }
}

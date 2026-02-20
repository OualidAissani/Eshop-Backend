using Eshop.Payement.Data;
using Eshop.Payement.Models;
using Eshop.Payement.Services.IServices;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;

namespace Eshop.Payement.Services
{
    public class PaymentService:IPaymentService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly PaymentDbContext _context;
        private readonly ILogger<PaymentService> _log;


        public PaymentService(IHttpClientFactory clientFactory, IConfiguration configuration, PaymentDbContext context, ILogger<PaymentService> log)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _context = context;
            _log = log;
        }

        public async Task<string> CreateOrder(List<Models.ItemsDto> items, Models.AmountDto amount)
        {
            var accessToken = await GetAccessToken();
            var client = _clientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-m.sandbox.paypal.com/v2/checkout/orders");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var data = OrderBodyDataMapping(items, amount);
            request.Content = JsonContent.Create(data);

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            await using var responseContent = await response.Content.ReadAsStreamAsync();
             using var orderResponse = await JsonDocument.ParseAsync(responseContent);

            return orderResponse.RootElement.GetProperty("links")
                .EnumerateArray()
                .FirstOrDefault(l => l.GetProperty("rel").GetString() == "payer-action")
                .GetProperty("href")
                .GetString();
        }

        private static global::System.Object OrderBodyDataMapping(List<Models.ItemsDto> items, Models.AmountDto amount)
        {
            var intent = "CAPTURE";
            var payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        user_action = "PAY_NOW",
                        return_url = "https://localhost:7294/api/payment/Return"

                    }
                }
            };
            var purchase_units = new[]
            {
                new
                {
                    items = items,
                    amount = new
                    {
                        currency_code = amount.currency_code,
                        value = amount.value,
                        breakdown = new
                        {
                            item_total = new
                            {
                                currency_code = amount.currency_code,
                                value = amount.value
                            }
                        }
                    }
                }
            };
            var data = new
            {
                payment_source,
                purchase_units,
                intent
            };
            return data;
        }

        public async Task<string> GetAccessToken()
        {
            var client = _clientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-m.sandbox.paypal.com/v1/oauth2/token");
            var clientId = _configuration["Paypal:ClientId"];
            var secretKey = _configuration["Paypal:SecretKey"];
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{secretKey}")));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });
            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

           await using var responseBody = await response.Content.ReadAsStreamAsync();

            using var tokenResponse = await JsonDocument.ParseAsync(responseBody);
            return tokenResponse.RootElement.GetProperty("access_token").GetString();
        }

        public async Task<JsonElement> GetOrderDetails(string orderId)
        {
            var accessToken = await GetAccessToken();

            var client = _clientFactory.CreateClient();

            var ChekcOrderRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api-m.sandbox.paypal.com/v2/checkout/orders/{orderId}");

            ChekcOrderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            ChekcOrderRequest.Content = new StringContent("", Encoding.UTF8, "application/json");

            var CheckingResponse = await client.SendAsync(ChekcOrderRequest);

            await using var CheckingResponseContent = await CheckingResponse.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<JsonElement>(CheckingResponseContent);
        }

        public async Task<object> RefundPayment(string captureId, AmountDto? amount,string userId)
        {
            if(captureId == null || amount == null ||  userId == null)
            {
                return null;
            }
            var paymentHistory = await _context.Payments.FirstOrDefaultAsync(i => i.CaptureId == captureId);

            if(paymentHistory == null)
            {
                return null;
            }

            if (paymentHistory.UserId != userId)
            {
                return null;
            }

            var accessToken = await GetAccessToken();

            var client = _clientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api-m.sandbox.paypal.com/v2/payments/captures/{captureId}/refund");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",accessToken);

            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            if(amount != null)
            {
                request.Content = JsonContent.Create(amount);
            }

            var response = await client.SendAsync(request);

            await using var responseContent = await response.Content.ReadAsStreamAsync();

            var DeserializedResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var status=DeserializedResponse.GetProperty("status").GetString();

            if (status!= "COMPLETED")
            {
                return null;
            }

            paymentHistory.Status = Status.Refunded;

            await _context.SaveChangesAsync();
            
            return DeserializedResponse;
        }

        //public async Task<int> Save(string payload)
        //{
        //    var receivedHook = JsonSerializer.Deserialize<JsonElement>(payload);
        //    if (receivedHook.GetProperty("resource").GetProperty("status").GetString() != "COMPLETED")
        //    {
        //        return 0;
        //    }
        //    var webhook = new Models.Webhook()
        //    {
        //        eventId = receivedHook.GetProperty("id").GetString(),
        //        event_type = receivedHook.GetProperty("event_type").GetString(),
        //        payload = payload
        //    };
        //    _context.WebhookLog.Add(webhook);
        //    return await _context.SaveChangesAsync();
        //}
        public async Task<int> CapturePayment(string orderId,string userId)
        {
            var accessToken = await GetAccessToken();

            var client = _clientFactory.CreateClient();

            var ChekcOrderRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api-m.sandbox.paypal.com/v2/checkout/orders/{orderId}");

            ChekcOrderRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            ChekcOrderRequest.Content=new StringContent("", Encoding.UTF8, "application/json");

            var CheckingResponse = await client.SendAsync(ChekcOrderRequest);

            await using var CheckingResponseContent = await CheckingResponse.Content.ReadAsStreamAsync();
            var Orderstatus = JsonSerializer.Deserialize<JsonElement>(CheckingResponseContent).GetProperty("status").GetString();
            if (!CheckingResponse.IsSuccessStatusCode||Orderstatus!= "APPROVED")
            {
                return 0;
            }
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api-m.sandbox.paypal.com/v2/checkout/orders/{orderId}/capture");

            request.Headers.Authorization=new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = new StringContent("",Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            await using var responseContent = await response.Content.ReadAsStreamAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError($"PayPal capture failed. StatusCode={response.StatusCode}");
                if (response.Headers.TryGetValues("PayPal-Debug-Id", out var dbg))
                    _log.LogError("PayPal-Debug-Id: " + string.Join(",", dbg));
                _log.LogError("PayPal body: " + responseContent);
                return 0;
            }
            using var deserializedResponse=await JsonDocument.ParseAsync(responseContent);


            var status=deserializedResponse.RootElement.GetProperty("purchase_units").EnumerateArray()
                .FirstOrDefault().GetProperty("payments").GetProperty("captures")
                .EnumerateArray().FirstOrDefault().GetProperty("status").GetString();

            if(response.IsSuccessStatusCode && status== "COMPLETED")
            {

                var CompletedOrder = new Models.Payment()
            {
                CaptureId=deserializedResponse.RootElement.GetProperty("purchase_units").EnumerateArray().FirstOrDefault().GetProperty("payments").GetProperty("captures")
                                    .EnumerateArray().FirstOrDefault().GetProperty("id").GetString(),
                OrderId = orderId,
                Status = Status.Captured,
                Amount = decimal.Parse(deserializedResponse.RootElement.GetProperty("purchase_units").EnumerateArray().FirstOrDefault().GetProperty("payments").GetProperty("captures")
                .EnumerateArray().FirstOrDefault().GetProperty("amount").GetProperty("value").GetString()),
                CapturedAt = DateTime.UtcNow,
                UserId=userId
            };

                _context.Payments.Add(CompletedOrder);

            }
            else 
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _log.LogError(errorContent);
            }
                        
            return await _context.SaveChangesAsync();
        }

        public async Task<JsonElement> GetCaptureDetails(string captureId)
        {
            var accessToken = await GetAccessToken();

            var client = _clientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api-m.sandbox.paypal.com/v2/payments/captures/{captureId}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            _log.LogError(responseContent);

            var captureDetails = JsonSerializer.Deserialize<JsonElement>(responseContent);


            return captureDetails;

        }

    }
}

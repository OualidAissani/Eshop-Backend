using Eshop.Payment.Data;
using Eshop.Payment.Models;
using Eshop.Payment.Services.IServices;
using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Eshop.Payment.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PaymentController:ControllerBase
    {
        private readonly IPaymentService _payementService;
        //private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentController(IPaymentService payementService,// IBackgroundJobClient backgroundClient,
            IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _payementService = payementService;
           // _backgroundJobClient = backgroundClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }


        [HttpGet("Return")]
        public async Task<IActionResult> Capture([FromQuery] string Token, [FromQuery] int orderId, [FromQuery] string correlationId)
        {
            var UserId= _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)??string.Empty;//to change
            var capturePayment = await _payementService.CapturePayment(Token, UserId, orderId, correlationId);
            if (capturePayment== 0)
            {
                return BadRequest("Payment capture failed");
            }
            return Ok();
        }
        [HttpGet("Capture")]
        public async Task<IActionResult> GetCaptureDetails([FromQuery] string captureId)
        {
            return Ok(await _payementService.GetCaptureDetails(captureId));
        }
        [HttpPost("Refund")]
        public async  Task<IActionResult> Refund([FromQuery] string CaptureId, [FromBody] AmountDto? amount)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await _payementService.RefundPayment(CaptureId,amount,userId));
        }



        //[HttpPost("Webhook")]
        //public async Task<IActionResult> Webhook()
        //{
        //    using var reader = new StreamReader(Request.Body);
        //    var body = await reader.ReadToEndAsync();
            
        //    if (string.IsNullOrEmpty(body))
        //    {
        //        return BadRequest("Empty body");
        //    }
        //    var isValid = await VerifyWebhookSignature(body);
        //    if (!isValid)
        //    {
        //        return BadRequest("Invalid signature");
        //    }
        //     await _payementService.Webhook(body);


        //    return Ok();
        //}

        //private async Task<bool> VerifyWebhookSignature(string body)
        //{
        //    var webhookId = _configuration["Paypal:WebhookId"]; 

        //    var verifyRequest = new
        //    {
        //        auth_algo = Request.Headers["PAYPAL-AUTH-ALGO"].ToString(),
        //        cert_url = Request.Headers["PAYPAL-CERT-URL"].ToString(),
        //        transmission_id = Request.Headers["PAYPAL-TRANSMISSION-ID"].ToString(),
        //        transmission_sig = Request.Headers["PAYPAL-TRANSMISSION-SIG"].ToString(),
        //        transmission_time = Request.Headers["PAYPAL-TRANSMISSION-TIME"].ToString(),
        //        webhook_id = webhookId,
        //        webhook_event = JsonSerializer.Deserialize<JsonElement>(body)
        //    };

        //    var accessToken = await _payementService.GetAccessToken();
        //    var client = _httpClientFactory.CreateClient();

        //    var request = new HttpRequestMessage(HttpMethod.Post,
        //        "https://api-m.sandbox.paypal.com/v1/notifications/verify-webhook-signature");
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //    request.Content = JsonContent.Create(verifyRequest);

        //    var response = await client.SendAsync(request);
        //    var result = await response.Content.ReadAsStringAsync();
        //    var json = JsonSerializer.Deserialize<JsonElement>(result);

        //    return json.GetProperty("verification_status").GetString() == "SUCCESS";
        //}
    }
}

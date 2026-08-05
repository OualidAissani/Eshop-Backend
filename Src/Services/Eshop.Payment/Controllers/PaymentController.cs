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
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentController(IPaymentService payementService,
            IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _payementService = payementService;     
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


    }
}

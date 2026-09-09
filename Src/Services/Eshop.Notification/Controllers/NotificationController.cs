using Eshop.Notification.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.Notification.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController:ControllerBase
    {
        private readonly IEmailService _emailService;
        public NotificationController(IEmailService emailService)
        {
            _emailService= emailService;
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail(string toEmail, string subject, string body, CancellationToken ct)
        {
            var result = await _emailService.SendEmailAsync(toEmail, subject, body, ct);

            return Ok(result);
        }

    }
}

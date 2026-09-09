using Eshop.Events;
using Eshop.Notification.Services.IServices;
using MassTransit;

namespace Eshop.Notification.EventHandler
{
    public class SendEmailEventConsumer : IConsumer<SendEmailEvent>
    {
        private readonly IEmailService _emailService;

        public SendEmailEventConsumer(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<SendEmailEvent> context)
        {
            var result = await _emailService.SendEmailAsync(context.Message.toEmail, context.Message.subject, context.Message.body, context.CancellationToken);

            return;
        }
    }
}

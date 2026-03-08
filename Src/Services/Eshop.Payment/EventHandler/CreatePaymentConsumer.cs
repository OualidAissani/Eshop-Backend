using Eshop.Events;
using Eshop.Payment.Services.IServices;
using MassTransit;

namespace Eshop.Payment.EventHandler
{
    public class CreatePaymentConsumer : IConsumer<CreatePaymentRecordRequest>
    {
        private readonly IPaymentService _paymentService;
        public CreatePaymentConsumer(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task Consume(ConsumeContext<CreatePaymentRecordRequest> context)
        {
            var message = context.Message;
            var itemDto = message.Items.Select(x => new Models.ItemsDto()
            {
                name = x.name,
                quantity = x.quantity,
                unit_amount = new Eshop.Payment.Models.AmountDto()
                {
                    currency_code = x.unit_amount?.currency_code ?? "USD",
                    value = x.unit_amount?.value.ToString("F2")
                }
                ,
                description = x.description
            }).ToList();



            var amount = new Models.AmountDto()
            {
                value = message.Amount.ToString(),
                currency_code = "USD"
            };

           var paymentUrl= await _paymentService.CreateOrder(itemDto, amount, message.OrderId, message.CorrelationId.ToString());

            if(paymentUrl == null)
            {
                await context.RespondAsync(new CreatePaymentRecordResponse
                {
                    PaymentUrl = null

                });
                return;
            }
            await context.RespondAsync(new CreatePaymentRecordResponse
            {
                PaymentUrl = paymentUrl

            });

        }
    }
}

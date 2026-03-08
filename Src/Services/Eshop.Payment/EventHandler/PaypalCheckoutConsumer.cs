using Eshop.Events;
using Eshop.Payment.Services.IServices;
using MassTransit;

namespace Eshop.Payment.EventHandler
{
    public class PaypalCheckoutConsumer : IConsumer<PaypalCheckout>
    {
        private readonly IPaymentService _payementService;

        public PaypalCheckoutConsumer(IPaymentService payementService)
        {
            _payementService = payementService;
        }

        public Task Consume(ConsumeContext<PaypalCheckout> context)
        {
            var message=context.Message;

            var items=message.items.Select(i=>new Models.ItemsDto
            {
                name=i.name,
                unit_amount=new Models.AmountDto
                {
                    currency_code=i.unit_amount.currency_code,
                    value=i.unit_amount.value.ToString("F2")
                },
                quantity =i.quantity
            }).ToList();
            var amount =new Models.AmountDto
            {
                currency_code=message.Amount.currency_code,
                value=message.Amount.value.ToString("F2")
            };

          //  _payementService.CreateOrder(items, amount);
            return Task.CompletedTask;
        }
    }
}

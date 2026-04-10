using Eshop.Events;
using Eshop.Orders.Services.IServices;
using MassTransit;

namespace Eshop.Orders.EventHandler
{
    public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
    {
        private readonly IOrderService _orderService;

        public OrderConfirmedConsumer(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task Consume(ConsumeContext<OrderConfirmed> context)
        {
            var message=context.Message;

           var result= await _orderService.OrderConfirmed(message.OrderId, context.CancellationToken);

            if (result.IsFailed)
            {
                throw new InvalidOperationException(
                    result.Errors.FirstOrDefault()?.Message);
            }
        }
    }
}

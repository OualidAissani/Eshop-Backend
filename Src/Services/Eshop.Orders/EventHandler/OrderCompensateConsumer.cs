using Eshop.Events;
using Eshop.Orders.Services;
using Eshop.Orders.Services.IServices;
using MassTransit;

namespace Eshop.Orders.EventHandler
{
    public class OrderCompensateConsumer : IConsumer<OrderCompensate>
    {
        private readonly IOrderService _orderService;

        public OrderCompensateConsumer(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task Consume(ConsumeContext<OrderCompensate> context)
        {
            var message = context.Message;
            CancellationToken ct=new CancellationToken();
            var result=await _orderService.DeleteOrder(message.OrderId,ct);
            if (result == false)
            {

            }
        }
    }
}

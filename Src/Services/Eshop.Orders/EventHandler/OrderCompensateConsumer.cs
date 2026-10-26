using Eshop.Events;
using Eshop.Orders.Services;
using Eshop.Orders.Services.IServices;
using MassTransit;
using System.Security.Claims;

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

            var result=await _orderService.DeleteOrder(message.OrderId,string.Empty,context.CancellationToken);
            if (result.IsFailed)
            {
                throw new InvalidOperationException(
                   result.Errors.FirstOrDefault()?.Message);
            }
        }
    }
}

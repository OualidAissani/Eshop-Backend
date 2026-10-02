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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderCompensateConsumer(IOrderService orderService, IHttpContextAccessor _httpContextAccessor)
        {
            _orderService = orderService;
            _httpContextAccessor = _httpContextAccessor;
        }

        public async Task Consume(ConsumeContext<OrderCompensate> context)
        {
            var message = context.Message;
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result=await _orderService.DeleteOrder(message.OrderId,userId,context.CancellationToken);
            if (result.IsFailed)
            {
                throw new InvalidOperationException(
                   result.Errors.FirstOrDefault()?.Message);
            }
        }
    }
}

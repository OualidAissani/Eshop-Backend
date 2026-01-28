using Eshop.Events;
using Eshop.Orders.Data;
using MassTransit;

namespace Eshop.Orders.EventHandler
{
    public class OrderProductsEvent:IConsumer<OrderProducts>
    {
        private readonly OrderDbContext _db;
        public OrderProductsEvent(OrderDbContext dbContext)
        {
            _db = dbContext;
        }

        public Task Consume(ConsumeContext<OrderProducts> context)
        {

            var productob = context.Message;
            return null;
        }
    }
}

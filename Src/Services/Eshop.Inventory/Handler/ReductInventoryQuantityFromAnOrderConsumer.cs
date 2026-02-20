using Eshop.Events;
using Eshop.Inventory.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Inventory.Handler
{
    public class ReductInventoryQuantityFromAnOrderConsumer : IConsumer<ReductInventoryQuantityFromAnOrder>
    {
        private readonly InventoryDb _db;
        private readonly IPublishEndpoint _publishEndpoint;
        public ReductInventoryQuantityFromAnOrderConsumer(InventoryDb inventoryDb, IPublishEndpoint publishEndpoint)
        {
            _db = inventoryDb;
            _publishEndpoint = publishEndpoint;
        }
        public async Task Consume(ConsumeContext<ReductInventoryQuantityFromAnOrder> context)
        {
            var message=context.Message;

            var productIds = message.Products.Select(p => p.ProductId).ToList();
            var inventories = await _db
                .Inventories
                .Where(i => productIds.Contains(i.ProductId))
                .ToListAsync();

            foreach (var inventory in inventories)
            {
                var dto = message.Products.FirstOrDefault(d => d.ProductId == inventory.ProductId);
                if (dto != null && inventory.Quantity > dto.Quantity)
                {
                    inventory.Quantity -= dto.Quantity;
                }
                else
                {
                    await _publishEndpoint.Publish(new OrderFailed { CorrelationId = message.CorrelationId });
                }

            }

            if (await _db.SaveChangesAsync() == 0)
            {
                await _publishEndpoint.Publish(new OrderFailed { CorrelationId = message.CorrelationId });
            }
            else
            {
                await _publishEndpoint.Publish(new InventoryReserved { CorrelationId = message.CorrelationId });
            }
        }
    }
}

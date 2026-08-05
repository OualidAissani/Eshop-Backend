using Eshop.Events;
using Eshop.Inventory.Data;
using Eshop.Inventory.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Inventory.Handler
{
    public class ReductInventoryQuantityFromAnOrderConsumer : IConsumer<ReductInventoryQuantityFromAnOrder>
    {
        private readonly InventoryDb _db;
        private readonly IInventoryService _inventoryService;
        private readonly IPublishEndpoint _publishEndpoint;
        public ReductInventoryQuantityFromAnOrderConsumer(InventoryDb inventoryDb, IPublishEndpoint publishEndpoint, IInventoryService inventoryService)
        {
            _db = inventoryDb;
            _publishEndpoint = publishEndpoint;
            _inventoryService = inventoryService;
        }
        public async Task Consume(ConsumeContext<ReductInventoryQuantityFromAnOrder> context)
        {
            var message = context.Message;

            var items = message.Products
                .Select(p => new Dtos.InventoryDto { ProductId = p.ProductId, Quantity = p.Quantity })
                .ToList();

            var insufficientProductIds = await _inventoryService.ReserveInventory(items, context.CancellationToken);

            if (insufficientProductIds.Count > 0)
            {
                await _publishEndpoint.Publish(new OrderFailed { CorrelationId = message.CorrelationId });
                return;
            }

            await _publishEndpoint.Publish(new InventoryReserved { CorrelationId = message.CorrelationId });
        }
    }
}

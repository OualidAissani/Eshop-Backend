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
            var message=context.Message;

            var productIds = message.Products.Select(p => p.ProductId).ToList();
            var inventories = await _inventoryService.GetInvetoriesByProductsIds(productIds);

            foreach (var inventory in inventories)
            {
                var dto = message.Products.FirstOrDefault(d => d.ProductId == inventory.ProductId);
                if (dto != null && inventory.Quantity >= dto.Quantity)
                {
                    inventory.Quantity -= dto.Quantity;
                }
                else
                {
                    await _publishEndpoint.Publish(new OrderFailed { CorrelationId = message.CorrelationId });
                }

            }
            var inventoryDto = inventories.Select(i=> new Dtos.InventoryDto
            {
                ProductId=i.ProductId,
                Quantity=i.Quantity
            }).ToList();
            if (await _inventoryService.UpdateQuantity(inventoryDto) !=productIds.Count())
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

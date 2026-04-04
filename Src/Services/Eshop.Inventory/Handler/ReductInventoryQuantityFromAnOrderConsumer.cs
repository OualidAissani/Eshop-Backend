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

            var productIds = message.Products.Select(p => p.ProductId).ToList();
            var inventories = await _inventoryService.GetInvetoriesByProductsIds(productIds);

            if (inventories.Count != productIds.Count)
            {
                await _publishEndpoint.Publish(new OrderFailed { CorrelationId = message.CorrelationId });
                return;
            }
            //var inventoryDictionary=inventories.ToDictionary(i => i.ProductId);


            //var dtos = new List<Models.Inventory>();


            var NewProductsValues = message.Products.ToDictionary(i=>i.ProductId);

            //var availableProductToReserve=NewProductsValues
            //    .Where(i=> inventoryDictionary
            //    .ContainsKey(i.Key) && inventoryDictionary[i.Key].Quantity >= i.Value.Quantity)
            //    .ToList();
            foreach(var item in inventories)
            {
                if(NewProductsValues.TryGetValue(item.ProductId,out var match))
                {
                    if (item.Quantity >= match.Quantity)
                    {
                        item.Quantity-=match.Quantity;
                    }
                    else
                    {
                        await _publishEndpoint.Publish(new OrderFailed { CorrelationId = message.CorrelationId });
                        return;
                    }
                }
            }

            var inventoryDto = inventories.Select(i => new Dtos.InventoryDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList();
            var result = await _inventoryService.UpdateQuantity(inventoryDto);
            if (result.Value != productIds.Count())
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

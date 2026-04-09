using Eshop.Events;
using Eshop.Inventory.Data;
using Eshop.Inventory.Services;
using FluentResults;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Inventory.Handler
{
    public class ProductInventoryQuanityConsumer:IConsumer<ProductInventoryAvailibityForOrderRequest>
    {

        private readonly IInventoryService _inventoryService;
        public ProductInventoryQuanityConsumer(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }
        public async Task Consume(ConsumeContext<ProductInventoryAvailibityForOrderRequest> context)
        {
            var message = context.Message;
            var productInventory=await _inventoryService.GetInvetoriesByProductsIds(message.ProductsId,context.CancellationToken);

            if(productInventory==null ||productInventory.Count == 0)
            {
                await context.RespondAsync(new ProductInventoryAvailibityForOrderResponse(null));
            }
            var items = productInventory.Select(p => new ProductInventoryItem(p.ProductId, p.Id, p.Quantity));

            await context.RespondAsync(new ProductInventoryAvailibityForOrderResponse(items));
        }
    }
}

using Eshop.Events;
using Eshop.Inventory.Data;
using FluentResults;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Inventory.Handler
{
    public class ProductInventoryQuanityConsumer:IConsumer<ProductInventoryAvailibityForOrderRequest>
    {

        private readonly InventoryDb _db;
        public ProductInventoryQuanityConsumer(InventoryDb inventoryDb)
        {
            _db = inventoryDb;
        }
        public async Task Consume(ConsumeContext<ProductInventoryAvailibityForOrderRequest> context)
        {
            var message = context.Message;
            var productInventory=await _db.Inventories.Where(i => message.ProductsId.Contains(i.ProductId)).ToListAsync();

            if(productInventory==null ||productInventory.Count == 0)
            {
                 Result.Fail("Product is not available in inventory");
                
            }
            var items = productInventory.Select(p => new ProductInventoryItem(p.ProductId, p.Id, p.Quantity));

            await context.RespondAsync(new ProductInventoryAvailibityForOrderResponse(items));
        }
    }
}

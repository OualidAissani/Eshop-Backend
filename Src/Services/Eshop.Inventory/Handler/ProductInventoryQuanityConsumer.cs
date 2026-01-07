using Eshop.Events;
using Eshop.Inventory.Data;
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
            var productInventoryQuantity=await _db.Inventories.Where(i => i.ProductId == message.Product).Select(s =>new
            {
                s.Quantity,
                s.Id
            }).FirstOrDefaultAsync();
            if (productInventoryQuantity == null)
            {
                await context.RespondAsync(new ProductInventoryAvailibityForOrderResponse(false, 0));
            }
            else if (productInventoryQuantity.Quantity < message.Quantity)
            {
                await context.RespondAsync(new ProductInventoryAvailibityForOrderResponse(false, productInventoryQuantity.Id));
            }
            else
            {
                await context.RespondAsync(new ProductInventoryAvailibityForOrderResponse(true, productInventoryQuantity.Id));
            }
        }
    }
}

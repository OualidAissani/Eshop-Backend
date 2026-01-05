using Eshop.Events;
using Eshop.Inventory.Data;
using MassTransit;

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
            var productInventoryQuantity=_db.Inventories.Where(i => i.ProductId == message.Product).Select(s =>new
            {
                s.Quantity,
                s.Id
            }).First();
            if(productInventoryQuantity.Quantity<message.Quantity)
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

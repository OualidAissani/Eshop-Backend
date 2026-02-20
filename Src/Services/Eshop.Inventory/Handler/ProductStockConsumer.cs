using Eshop.Events;
using Eshop.Inventory.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Inventory.Handler
{
    public class ProductStockConsumer : IConsumer<ProductStockRequest>
    {
        private readonly InventoryDb _dbcontext;
        public ProductStockConsumer(InventoryDb dbcontext)
        {
            _dbcontext = dbcontext;
        }
        public async Task Consume(ConsumeContext<ProductStockRequest> context)
        {
            var message = context.Message;

            var stock = await _dbcontext.Inventories.AnyAsync(i => i.ProductId == message.ProductsId && i.Quantity >= message.Quantity);
            await context
                .RespondAsync(new ProductStockResponse(stock));
        }
    }
}

using Eshop.Events;
using Eshop.Inventory.Services;
using MassTransit;

namespace Eshop.Inventory.Handler
{
    public class DeleteProductInventory : IConsumer<DeleteInventory>
    {
        private readonly IInventoryService _inventoryService;

        public DeleteProductInventory(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public async Task Consume(ConsumeContext<DeleteInventory> context)
        {
           await _inventoryService.DeleteInventoryByProductId(context.Message.productId, CancellationToken.None );
        }
    }
}

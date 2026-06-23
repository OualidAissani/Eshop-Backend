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

        public Task Consume(ConsumeContext<DeleteInventory> context)
        {
            _inventoryService.DeleteInventoryByProductId(context.Message.productId, CancellationToken.None );
            return Task.CompletedTask;
        }
    }
}

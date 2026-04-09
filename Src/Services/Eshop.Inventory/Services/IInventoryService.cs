using FluentResults;

namespace Eshop.Inventory.Services
{
    public interface IInventoryService
    {
        Task<Result<Models.Inventory>> CreateInventoryForProduct(Dtos.InventoryDto Inventory, CancellationToken ct);
        Task<List<Models.Inventory>> GetAllInventories(CancellationToken ct);
        Task<Result<int>> PushUpdatetOdB(List<Models.Inventory> inventories, CancellationToken ct);
        Task<Models.Inventory?> GetInventoryById(int InventoryId, CancellationToken ct);
        Task<Result<Models.Inventory>> UpdateInventory(Dtos.InventoryDto inventoryDto, CancellationToken ct);
        Task<Result<int>> UpdateQuantity(List<Dtos.InventoryDto> invDto, CancellationToken ct);
        Task<Result<bool?>> DeleteInventory(int InventoryId, CancellationToken ct);
        Task<List<Models.Inventory>> GetInvetoriesByProductsIds(List<int> productIds, CancellationToken ct);

    }
}

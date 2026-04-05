using FluentResults;

namespace Eshop.Inventory.Services
{
    public interface IInventoryService
    {
        Task<Models.Inventory> CreateInventoryForProduct(Dtos.InventoryDto Inventory);
        Task<List<Models.Inventory>> GetAllInventories();
        Task<Result<int>> PushUpdatetOdB(List<Models.Inventory> inventories);
        Task<Models.Inventory?> GetInventoryById(int InventoryId);
        Task<Result<Models.Inventory>> UpdateInventory(Dtos.InventoryDto inventoryDto);
        Task<Result<int>> UpdateQuantity(List<Dtos.InventoryDto> invDto);
        Task<Result<bool?>> DeleteInventory(int InventoryId);
        Task<List<Models.Inventory>> GetInvetoriesByProductsIds(List<int> productIds);

    }
}

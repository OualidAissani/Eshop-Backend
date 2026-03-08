using Eshop.Inventory.Dtos;

namespace Eshop.Inventory.Services
{
    public interface IInventoryService
    {
        Task<Models.Inventory> CreateInventoryForProduct(Dtos.InventoryDto Inventory);
        Task<List<Models.Inventory>> GetAllInventories();
        Task<Models.Inventory?> GetInventoryById(int InventoryId);
        Task<Models.Inventory> UpdateInventory(Dtos.InventoryDto inventoryDto);
        Task<int> UpdateQuantity(List<Dtos.InventoryDto> invDto);
        Task<bool?> DeleteInventory(int InventoryId);
        Task<List<Models.Inventory>> GetInvetoriesByProductsIds(List<int> productIds);

    }
}

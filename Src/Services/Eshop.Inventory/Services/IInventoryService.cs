using Eshop.Inventory.Dtos;

namespace Eshop.Inventory.Services
{
    public interface IInventoryService
    {
        Task<Models.Inventory> CreateInvetoryForProduct(InventoryDto Inventory);
        Task<List<Models.Inventory>> GetAllInventories();
        Task<Models.Inventory?> GetInventoryById(int InventoryId);
        Task<bool> UpdateInventory(int InventoryId, int Count, bool Incresed);
        Task<bool?> DeleteInventory(int InventoryId);
    }
}

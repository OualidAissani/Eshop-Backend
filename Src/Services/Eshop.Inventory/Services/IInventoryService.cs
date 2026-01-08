using Eshop.Inventory.Dtos;

namespace Eshop.Inventory.Services
{
    public interface IInventoryService
    {
        Task<Models.Inventory> CreateInvetoryForProduct(InventoryDto Inventory);
        Task<List<Models.Inventory>> GetAllInventories();
        Task<Models.Inventory?> GetInventoryById(int InventoryId);
        Task<Models.Inventory> UpdateInventory(int InventoryId, InventoryDto inventoryDto);
        Task<List<Models.Inventory>> UpdatePrice(List<InventoryDto> invDto);
        Task<bool?> DeleteInventory(int InventoryId);

    }
}

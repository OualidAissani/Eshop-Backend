using Eshop.Events;
using Eshop.Inventory.Data;
using Eshop.Inventory.Dtos;
using Eshop.Inventory.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Inventory.Services
{
    public class InventoryService:IInventoryService
    {
        private readonly IRequestClient<VerifyProductExistence> _Client;
        private readonly InventoryDb _db;
        public InventoryService(IRequestClient<VerifyProductExistence> Client,InventoryDb db)
        {
            _Client = Client;
            _db = db;
        }
        public async Task<List<Models.Inventory>> GetAllInventories()
        {
            return await _db.Inventories.AsNoTracking().ToListAsync();
        }
        public async Task<Models.Inventory?> GetInventoryById(int InventoryId)
        {
            return await _db.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.Id == InventoryId);
        }
        public async Task<Models.Inventory> CreateInvetoryForProduct(InventoryDto Inventory)
        {
            var response=await _Client.GetResponse<ProductExistenceResponse>(new VerifyProductExistence(Inventory.ProductId));
            if (response.Message.Exists == false)
            {
                return null;
            }
            var inventory=new Models.Inventory
            {
               ProductId=Inventory.ProductId,
                Quantity=Inventory.Quantity
            };
            _db.Inventories.Add(inventory);
            await _db.SaveChangesAsync();
            return inventory;
        }
        public async Task<Models.Inventory> UpdateInventory(int InventoryId,InventoryDto inventoryDto)
        {
            var inventory = await _db.Inventories.FindAsync(InventoryId);
            inventory.Quantity = inventoryDto.Quantity;
            inventory.ProductId = inventoryDto.ProductId;
            _db.Inventories.Update(inventory);
            await _db.SaveChangesAsync();

            return inventory;
        }
        public async Task<List<Models.Inventory>> UpdatePrice(List<InventoryDto> invDto)
        {
            var productIds = invDto.Select(p => p.ProductId).ToList();
            var inventories = await _db
                .Inventories
                .Where(i => productIds.Contains(i.ProductId))
                .ToListAsync();

            foreach (var inventory in inventories)
            {
                var dto = invDto.FirstOrDefault(d => d.ProductId == inventory.ProductId);
                if (dto != null)
                {
                    inventory.Quantity -= dto.Quantity;
                }
            }

            await _db.SaveChangesAsync();

            return inventories;
        }
        public async Task<bool?> DeleteInventory(int InventoryId)
        {
            try
            {
                _db.Inventories.Remove(new Models.Inventory { Id = InventoryId });
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}

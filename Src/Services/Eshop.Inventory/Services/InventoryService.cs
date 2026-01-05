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
        public async Task<bool> UpdateInventory(int InventoryId,int Count,bool Incresed)
        {
            var inv = await _db.Inventories.FindAsync(InventoryId);
            if (Count <= 0 || inv == null)
            {
                return false;
            }
                if (Incresed)
                {
                    inv.Quantity += Count;
                }
                else
                {
                    inv.Quantity -= Count;

                }
            
            _db.Inventories.Update(inv);
            await _db.SaveChangesAsync();

            return true;
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

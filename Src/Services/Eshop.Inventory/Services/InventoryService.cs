using Eshop.Events;
using Eshop.Inventory.Data;
using Eshop.Inventory.Dtos;
using Eshop.Inventory.Models;
using FluentResults;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Inventory.Services
{
    public class InventoryService:IInventoryService
    {
        private readonly IRequestClient<VerifyProductExistence> _Client;
        private readonly InventoryDb _db;
        private readonly ILogger<InventoryService> _logger;
        public InventoryService(IRequestClient<VerifyProductExistence> Client, InventoryDb db, ILogger<InventoryService> logger)
        {
            _Client = Client;
            _db = db;
            _logger = logger;
        }
        public async Task<List<Models.Inventory>> GetAllInventories()
        {
            return await _db.Inventories.AsNoTracking().ToListAsync();
        }
        public async Task<Models.Inventory?> GetInventoryById(int InventoryId)
        {
            return await _db.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.Id == InventoryId);
        }
        public async Task<Models.Inventory> CreateInventoryForProduct(Dtos.InventoryDto Inventory)
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
        public async Task<Result<Models.Inventory>> UpdateInventory(Dtos.InventoryDto inventoryDto)
        {
            if(inventoryDto==null || inventoryDto.Quantity < 0|| inventoryDto.ProductId <= 0)
            {
                throw new ArgumentNullException();
            }
            
            var inventory = await _db.Inventories.Where(i=>i.ProductId==inventoryDto.ProductId).FirstOrDefaultAsync();


            if (inventory == null) return Result.Fail($"Inventory For Product With Id {inventory.ProductId} Not Found");


            inventory.Quantity = inventoryDto.Quantity;
            inventory.ProductId = inventoryDto.ProductId;
            _db.Inventories.Update(inventory);

            if(await _db.SaveChangesAsync()==0)
            {
                return Result.Fail("Error Updating Inventory Try Again Later");
            }

            return inventory;
        }
        public async Task<Result<int>> UpdateQuantity(List<Dtos.InventoryDto> invDto)
        {
            if (invDto == null || invDto.Count == 0)
                throw new ArgumentNullException();

            int totalUpdated = 0;

            foreach (var item in invDto)
            {
                totalUpdated += await _db.Inventories
                    .Where(i => i.ProductId == item.ProductId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(i => i.Quantity, item.Quantity));
            }
            if (totalUpdated == 0)
            {
                return Result.Fail("Error Updating Inventory Try Again Later");
            }
            return totalUpdated;
        }
        public async Task<Result<bool?>> DeleteInventory(int InventoryId)
        {
            ArgumentNullException.ThrowIfNull(InventoryId);
            var inventory = await _db.Inventories.FindAsync(InventoryId);
            if (inventory == null)
            {
                return Result.Fail($"The Inventory with ID {InventoryId} Not Found");
            }

            try
            {
                _db.Inventories.Remove(inventory);

                if(await _db.SaveChangesAsync()==0)
                {
                    return Result.Fail("There was an issue Deleting The Inventory");
                }
                return true;
            }
            catch (DbUpdateConcurrencyException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public async Task<List<Models.Inventory>> GetInvetoriesByProductsIds(List<int> productIds)
        {
            return await _db
                .Inventories
                .Where(i => productIds.Contains(i.ProductId))
                .ToListAsync();
        }
    }
}

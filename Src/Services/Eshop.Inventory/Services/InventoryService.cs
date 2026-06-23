using Eshop.Events;
using Eshop.Inventory.Data;
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
        public async Task<List<Models.Inventory>> GetAllInventories(CancellationToken ct)
        {
            return await _db.Inventories.AsNoTracking().ToListAsync(ct);
        }
        public async Task<Models.Inventory?> GetInventoryById(int InventoryId, CancellationToken ct)
        {
            return await _db.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.Id == InventoryId, ct);
        }
        public async Task<Result<Models.Inventory>> CreateInventoryForProduct(Dtos.InventoryDto Inventory, CancellationToken ct)
        {
            var response=await _Client.GetResponse<ProductExistenceResponse>(new VerifyProductExistence(Inventory.ProductId));
            if (response.Message.Exists == false)
            {
                return Result.Fail<Models.Inventory>("The Product Doesnt Exist");
            }
            
            var inventory = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == Inventory.ProductId);
            if (inventory != null)
            {
                inventory.Quantity += Inventory.Quantity;
            }
            else
            {

                 inventory = new Models.Inventory
                {
                    ProductId = Inventory.ProductId,
                    Quantity = Inventory.Quantity
                };
                _db.Inventories.Add(inventory);
            }

            if(await _db.SaveChangesAsync(ct)==0)
            {
                return Result.Fail("Error Creating Inventory Try Again Later");
            }
            return inventory;
        }
        public async Task<Result<Models.Inventory>> UpdateInventory(Dtos.InventoryDto inventoryDto, CancellationToken ct)
        {
            if(inventoryDto==null || inventoryDto.Quantity < 0|| inventoryDto.ProductId <= 0)
            {
                throw new ArgumentNullException();
            }

            var inventory = await _db.Inventories.Where(i=>i.ProductId==inventoryDto.ProductId).FirstOrDefaultAsync(ct);


            if (inventory == null) return Result.Fail($"Inventory For Product With Id {inventoryDto.ProductId} Not Found");


            inventory.Quantity = inventoryDto.Quantity;
            inventory.ProductId = inventoryDto.ProductId;
            _db.Inventories.Update(inventory);

            if(await _db.SaveChangesAsync(ct)==0)
            {
                return Result.Fail("Error Updating Inventory Try Again Later");
            }

            return inventory;
        }
        public async Task<Result<int>> UpdateQuantity(List<Dtos.InventoryDto> invDto, CancellationToken ct)
        {
            if (invDto == null || invDto.Count == 0)
                throw new ArgumentNullException();

            int totalUpdated = 0;

            var invs = await _db.Inventories
                    .Where(i => invDto.Select(d=>d.ProductId).Contains(i.ProductId)).ToListAsync(ct);

            var invDtopDictionary = invDto.ToDictionary(i => i.ProductId);

            foreach (var item in invs)
            {
                item.Quantity = invDtopDictionary[item.ProductId].Quantity;
            }
            totalUpdated = await _db.SaveChangesAsync(ct);
            if (totalUpdated == 0)
            {
                return Result.Fail("Error Updating Inventory Try Again Later");
            }
            return totalUpdated;
        }
        public async Task<Result<int>> PushUpdatetOdB(List<Models.Inventory> inventories, CancellationToken ct)
        {
            if (inventories == null || inventories.Count == 0)
                throw new ArgumentNullException();


            _db.Inventories.UpdateRange(inventories);
            int totalUpdated = await _db.SaveChangesAsync(ct);
            if (totalUpdated == 0)
            {
                return Result.Fail("Error Updating Inventory Try Again Later");
            }
            return totalUpdated;
        }
        public async Task<Result<bool?>> DeleteInventory(int InventoryId, CancellationToken ct)
        {
            if (InventoryId <= 0)
            {
                throw new ArgumentNullException("Inventory Id is not valid");
            }
            var inventory = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == InventoryId, ct);
            if (inventory == null)
            {
                return Result.Fail($"The Inventory with ID {InventoryId} Not Found");
            }

            try
            {
                _db.Inventories.Remove(inventory);

                if(await _db.SaveChangesAsync(ct)==0)
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

        public async Task<Result<bool?>> DeleteInventoryByProductId(int productId, CancellationToken ct)
        {
            if (productId <= 0)
            {
                throw new ArgumentNullException("Product Id is not valid");
            }
            var inventory = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId, ct);
            if (inventory == null)
            {
                return true;
            }

            try
            {
                _db.Inventories.Remove(inventory);

                if (await _db.SaveChangesAsync(ct) == 0)
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

        public async Task<List<Models.Inventory>> GetInvetoriesByProductsIds(List<int> productIds, CancellationToken ct)
        {
            return await _db
                .Inventories
                .Where(i => productIds.Contains(i.ProductId))
                .ToListAsync(ct);
        }
    }
}

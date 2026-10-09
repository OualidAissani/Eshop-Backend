using Eshop.Events;
using Eshop.Inventory.Dtos;
using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using REDox.Json;
using StackExchange.Redis;

namespace Eshop.Inventory.Services
{
    public class CachedInventoryService : IInventoryService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IDatabase _redisDb;
        public CachedInventoryService(IDatabase cache, IInventoryService inventoryService)
        {
            _redisDb = cache;
            _inventoryService = inventoryService;
        }
        public async Task<Result<Models.Inventory>> CreateInventoryForProduct(Dtos.InventoryDto Inventory, CancellationToken ct)
        {
            if (Inventory.IdempontencyKey == null)
            {
                return Result.Fail("");
            }
            var cacheKey = $"Idempotency:Inventory:Create:{Inventory.IdempontencyKey}";
            var reserved = await _redisDb.StringSetAsync(cacheKey, "in-progress", TimeSpan.FromHours(24), When.NotExists);
            if (!reserved)
            {
                var cached=await _redisDb.StringGetAsync(cacheKey);
                if (cached =="in-progress")
                {
                    return Result.Fail("Request already in progress"); // or 409

                }
                return JsonSerializer.Deserialize<Models.Inventory>(cached.ToString()) ?? null;
            }


            var inventory = await _inventoryService.CreateInventoryForProduct(Inventory, ct);

            if (inventory.IsFailed)
            {
                return Result.Fail("Failed to create inventory");
            }

            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(inventory.Value),TimeSpan.FromHours(24));

            await _redisDb.KeyDeleteAsync("Inventories:All");

            return inventory;
        }

        public async Task<Result<bool?>> DeleteInventory(int InventoryId, CancellationToken ct)
        {
            var result = await _inventoryService.DeleteInventory(InventoryId, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }

            await _redisDb.KeyDeleteAsync($"Inventory:{InventoryId}");

            await _redisDb.KeyDeleteAsync("Inventories:All");
            return true;
        }

        public async Task<Result<bool?>> DeleteInventoryByProductId(int productId, CancellationToken ct)
        {
             var result=await _inventoryService.DeleteInventoryByProductId(productId, ct);
            await _redisDb.KeyDeleteAsync($"Inventories:All");

            return result;
        }

        public async Task<List<Models.Inventory>> GetAllInventories(CancellationToken ct)
        {
            var cacheKey = "Inventories:All";
            var cached = await _redisDb.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                return JsonSerializer.Deserialize<List<Models.Inventory>>(cached.ToString());
            }
            var inventories = await _inventoryService.GetAllInventories(ct);
            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(inventories), TimeSpan.FromHours(24));
            return inventories;
        }

        public async Task<Models.Inventory?> GetInventoryById(int InventoryId, CancellationToken ct)
        {
            var cacheKey = $"Inventory:{InventoryId}";
            var cached = await _redisDb.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                return JsonSerializer.Deserialize<Models.Inventory>(cached.ToString());
            }
            var inventory = await _inventoryService.GetInventoryById(InventoryId, ct);
            if (inventory == null)
            {
                return null;
            }
            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(inventory),TimeSpan.FromHours(24));
            return inventory;
        }

        public async Task<List<Models.Inventory>> GetInvetoriesByProductsIds(List<int> productIds, CancellationToken ct)
        {
            return await _inventoryService.GetInvetoriesByProductsIds(productIds, ct);
        }

        public async Task<List<int>> ReserveInventory(List<Dtos.InventoryDto> items, CancellationToken ct)
        {
            var result= await _inventoryService.ReserveInventory(items, ct);
            await _redisDb.KeyDeleteAsync($"Inventories:All");

            return result;
        }

        public async Task<Result<Models.Inventory>> UpdateInventory(Dtos.InventoryDto inventoryDto, CancellationToken ct)
        {
            if (inventoryDto.IdempontencyKey == null)
            {
                return Result.Fail("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Inventory:Update:{inventoryDto.IdempontencyKey}";
            var cached = await _redisDb.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                return JsonSerializer.Deserialize<Models.Inventory>(cached.ToString());
            }
            var result = await _inventoryService.UpdateInventory(inventoryDto, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }
            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromHours(24));
            await _redisDb.KeyDeleteAsync($"Inventory:{result.Value.Id}");
            await _redisDb.KeyDeleteAsync("Inventories:All");
            return result.Value;
        }

        public async Task<Result<int>> UpdateQuantity(UpdateQuantityRequest invDto, CancellationToken ct)
        {
            if (invDto.IdempotencyKey== null)
            {
                return Result.Fail("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Inventory:UpdateQuantity:{invDto.IdempotencyKey}";
            var cached = await _redisDb.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                return JsonSerializer.Deserialize<int>(cached.ToString());
            }
            var result = await _inventoryService.UpdateQuantity(invDto, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }

            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(result.Value),TimeSpan.FromHours(24));
            await _redisDb.KeyDeleteAsync("Inventories:All");
            return result.Value;
        }
    }
}

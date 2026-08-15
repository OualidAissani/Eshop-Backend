using Eshop.Events;
using Eshop.Inventory.Dtos;
using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Eshop.Inventory.Services
{
    public class CachedInventoryService : IInventoryService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IDistributedCache _cache;
        public CachedInventoryService(IDistributedCache cache, IInventoryService inventoryService)
        {
            _cache = cache;
            _inventoryService = inventoryService;
        }
        public async Task<Result<Models.Inventory>> CreateInventoryForProduct(Dtos.InventoryDto Inventory, CancellationToken ct)
        {
            if (Inventory.IdempontencyKey == null)
            {
                return Result.Fail("");
            }
            var cacheKey = $"Idempotency:Inventory:Create:{Inventory.IdempontencyKey}";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<Models.Inventory>(cached) ?? null;
            }

            var inventory = await _inventoryService.CreateInventoryForProduct(Inventory, ct);

            if (inventory.IsFailed)
            {
                return Result.Fail("");
            }

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(inventory), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            await _cache.RemoveAsync("Inventories:All");

            return inventory;
        }

        public async Task<Result<bool?>> DeleteInventory(int InventoryId, CancellationToken ct)
        {
            var result = await _inventoryService.DeleteInventory(InventoryId, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }

            await _cache.RemoveAsync($"Inventory:{InventoryId}");

            await _cache.RemoveAsync("Inventories:All");
            return true;
        }

        public async Task<Result<bool?>> DeleteInventoryByProductId(int productId, CancellationToken ct)
        {
             var result=await _inventoryService.DeleteInventoryByProductId(productId, ct);
            await _cache.RemoveAsync($"Inventories:All");

            return result;
        }

        public async Task<List<Models.Inventory>> GetAllInventories(CancellationToken ct)
        {
            var cacheKey = "Inventories:All";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<List<Models.Inventory>>(cached);
            }
            var inventories = await _inventoryService.GetAllInventories(ct);
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(inventories), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            return inventories;
        }

        public async Task<Models.Inventory?> GetInventoryById(int InventoryId, CancellationToken ct)
        {
            var cacheKey = $"Inventory:{InventoryId}";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<Models.Inventory>(cached);
            }
            var inventory = await _inventoryService.GetInventoryById(InventoryId, ct);
            if (inventory == null)
            {
                return null;
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(inventory), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return inventory;
        }

        public async Task<List<Models.Inventory>> GetInvetoriesByProductsIds(List<int> productIds, CancellationToken ct)
        {
            return await _inventoryService.GetInvetoriesByProductsIds(productIds, ct);
        }

        public async Task<List<int>> ReserveInventory(List<Dtos.InventoryDto> items, CancellationToken ct)
        {
            var result= await _inventoryService.ReserveInventory(items, ct);
            await _cache.RemoveAsync($"Inventories:All");

            return result;
        }

        public async Task<Result<Models.Inventory>> UpdateInventory(Dtos.InventoryDto inventoryDto, CancellationToken ct)
        {
            if (inventoryDto.IdempontencyKey == null)
            {
                return Result.Fail("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Inventory:Update:{inventoryDto.IdempontencyKey}";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<Models.Inventory>(cached);
            }
            var result = await _inventoryService.UpdateInventory(inventoryDto, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            await _cache.RemoveAsync($"Inventory:{result.Value.Id}");
            await _cache.RemoveAsync("Inventories:All");
            return result.Value;
        }

        public async Task<Result<int>> UpdateQuantity(UpdateQuantityRequest invDto, CancellationToken ct)
        {
            if (invDto.IdempotencyKey== null)
            {
                return Result.Fail("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Inventory:UpdateQuantity:{invDto.IdempotencyKey}";
            var cached = await _cache.GetAsync(cacheKey);

            if (cached != null)
            {
                return JsonSerializer.Deserialize<int>(cached);
            }
            var result = await _inventoryService.UpdateQuantity(invDto, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result.Value), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            await _cache.RemoveAsync("Inventories:All");
            return result.Value;
        }
    }
}

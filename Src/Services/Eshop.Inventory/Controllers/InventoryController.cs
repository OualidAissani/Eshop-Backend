using Eshop.Inventory.Data;
using Eshop.Inventory.Dtos;
using Eshop.Inventory.Services;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Eshop.Inventory.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly IDistributedCache _cache;
        public InventoryController(IInventoryService inventoryService,IDistributedCache cache)
        {
            _inventoryService = inventoryService;
            _cache = cache;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllInventories( CancellationToken ct)
        {
            var cacheKey="Inventories:All";
            var cached = await _cache.GetAsync(cacheKey);
            if(cached != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Models.Inventory>>(cached));
            }
            var inventories = await _inventoryService.GetAllInventories(ct);
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(inventories), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            return Ok(inventories);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInventoryById(int id, CancellationToken ct)
        {
            var cacheKey = $"Inventory:{id}";
            var cached = await _cache.GetAsync(cacheKey);
            if(cached != null)
            {
                return Ok(JsonSerializer.Deserialize<Models.Inventory>(cached));
            }
            var inventory = await _inventoryService.GetInventoryById(id,ct);
            if (inventory == null)
            {
                return NotFound();
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(inventory), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return Ok(inventory);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInventory([FromBody] InventoryDto inventoryDto, CancellationToken ct, [FromHeader(Name = "x_Idempotency_Key")] string key)
        {
            if (key == null)
            {
                return BadRequest();
            }
            var cacheKey = $"Idempotency:Inventory:Create:{key}";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return CreatedAtAction(nameof(GetInventoryById), new { id = JsonSerializer.Deserialize<Models.Inventory>(cached)?.Id }, JsonSerializer.Deserialize<Models.Inventory>(cached) ?? null);
            }

            var inventory = await _inventoryService.CreateInventoryForProduct(inventoryDto,ct);

            if (inventory.IsFailed)
            {
                return BadRequest("");
            }

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(inventory), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            await _cache.RemoveAsync("Inventories:All");

            return CreatedAtAction(nameof(GetInventoryById), new { id = inventory?.Value.Id }, inventory.Value);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventory(int id, CancellationToken ct)
        {
            var result = await _inventoryService.DeleteInventory(id, ct);
            if (result.IsFailed)
            {
                return NotFound(result.Errors.First().Message);
            }
            
            await _cache.RemoveAsync($"Inventory:{id}");

            await _cache.RemoveAsync("Inventories:All");
            return NoContent();
        }
        [HttpPut]
        public async Task<IActionResult> UpdateInventory([FromBody] InventoryDto inventoryDto, CancellationToken ct, [FromHeader(Name = "x_Idempotency_Key")] string key)
        {
            if (key == null)
            {
                return BadRequest("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Inventory:Update:{key}";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<Models.Inventory>(cached));
            }
            var result = await _inventoryService.UpdateInventory(inventoryDto, ct);
            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            await _cache.RemoveAsync($"Inventory:{result.Value.Id}");
            await _cache.RemoveAsync("Inventories:All");
            return Ok(result.Value);
        }
        [HttpPut("UpdateQuantity")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateQuantity([FromBody] List<InventoryDto> invDto, CancellationToken ct, [FromHeader(Name = "x_Idempotency_Key")] string key)
        {
            if(key == null)
            {
                return BadRequest("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Inventory:UpdateQuantity:{key}";
            var cached = await _cache.GetAsync(cacheKey);

            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Models.Inventory>>(cached));
            }
            var result = await _inventoryService.UpdateQuantity(invDto,ct);
            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
            await _cache.RemoveAsync("Inventories:All");
            return Ok(result.Value);
        }
    }
}

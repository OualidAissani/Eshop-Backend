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
        public async Task<IActionResult> GetAllInventories()
        {
            var inventories = await _inventoryService.GetAllInventories();
            return Ok(inventories);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInventoryById(int id)
        {
            var inventory = await _inventoryService.GetInventoryById(id);
            if (inventory == null)
            {
                return NotFound();
            }
            return Ok(inventory);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateInventory([FromBody] InventoryDto inventoryDto, [FromHeader(Name = "x_Idempotency_Key")] string key)
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
            var inventory = await _inventoryService.CreateInvetoryForProduct(inventoryDto);
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(inventory), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return CreatedAtAction(nameof(GetInventoryById), new { id = inventory?.Id }, inventory?? null);
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            var result = await _inventoryService.DeleteInventory(id);
            if (result == null)
            {
                return NotFound();
            }
            if (result == false)
            {
                return BadRequest();
            }
            return NoContent();
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] InventoryDto inventoryDto,[FromHeader(Name = "x_Idempotency_Key")] string key)
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
            var result = await _inventoryService.UpdateInventory(id,inventoryDto);
            if (result == null)
            {
                return BadRequest("Inventory not found");
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return Ok(result);
        }
        [HttpPut("UpdatePrice")]
        //Change later
        public async Task<IActionResult> UpdatePrice([FromBody] List<InventoryDto> invDto, [FromHeader(Name = "x_Idempotency_Key")] string key)
        {
            if(key == null)
            {
                return BadRequest("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Inventory:UpdatePrice:{key}";
            var cached = await _cache.GetAsync(cacheKey);

            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Models.Inventory>>(cached));
            }
            var result = await _inventoryService.UpdatePrice(invDto);
            if (result == null)
            {
                return BadRequest("No Change Happened");
            }

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
            
            return Ok(result);
        }
    }
}

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
        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }
        [AllowAnonymous]
        [HttpGet]
       
        public async Task<IActionResult> GetAllInventories( CancellationToken ct)
        {
           
            var inventories = await _inventoryService.GetAllInventories(ct);
           
            return Ok(inventories);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInventoryById(int id, CancellationToken ct)
        {
          
            var inventory = await _inventoryService.GetInventoryById(id,ct);
            if (inventory == null)
            {
                return NotFound();
            }
            
            return Ok(inventory);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInventory([FromBody] InventoryDto inventoryDto, CancellationToken ct,
            [FromHeader(Name = "x-Idempotency-Key")] string key)
        {
           inventoryDto.IdempontencyKey= key;
            var inventory = await _inventoryService.CreateInventoryForProduct(inventoryDto,ct);

            if (inventory.IsFailed)
            {
                return BadRequest("");
            }

            return await GetAllInventories(ct);
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
            
            return NoContent();
        }
        [HttpPut]
        public async Task<IActionResult> UpdateInventory([FromBody] InventoryDto inventoryDto, CancellationToken ct,
            [FromHeader(Name = "x-Idempotency-Key")] string key)
        {

            if (key == null)
            {
                return BadRequest("Idempotency Key is required");
            }
            inventoryDto.IdempontencyKey= key;
            var result = await _inventoryService.UpdateInventory(inventoryDto, ct);
            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }
            
            return Ok(result.Value);
        }
        [HttpPut("UpdateQuantity")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest invDto, CancellationToken ct,
            [FromHeader(Name = "x-Idempotency-Key")] string key)
        {
            if(key == null)
            {
                return BadRequest("Idempotency Key is required");
            }
            
            invDto.IdempotencyKey= key;
            var result = await _inventoryService.UpdateQuantity(invDto,ct);
            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);   
            }

            return Ok(result.Value);
        }
    }
}

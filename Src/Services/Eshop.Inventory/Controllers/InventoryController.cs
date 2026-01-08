using Eshop.Inventory.Data;
using Eshop.Inventory.Dtos;
using Eshop.Inventory.Services;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.Inventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
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
        public async Task<IActionResult> CreateInventory([FromBody] InventoryDto inventoryDto)
        {
            var inventory = await _inventoryService.CreateInvetoryForProduct(inventoryDto);
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
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] InventoryDto inventoryDto)
        {
            var result = await _inventoryService.UpdateInventory(id,inventoryDto);
            if (result == null)
            {
                Result.Fail("Inventory not found");
                return BadRequest();
            }
            return Ok(result);
        }
        [HttpPut("UpdatePrice")]
        public async Task<IActionResult> UpdatePrice([FromBody] List<InventoryDto> invDto)
        {
            var result = await _inventoryService.UpdatePrice(invDto);
            if (result == null)
            {
                Result.Fail("No Change Happened");
                return BadRequest();
            }
            return Ok(result);
        }
    }
}

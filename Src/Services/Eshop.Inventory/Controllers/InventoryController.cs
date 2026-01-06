using Eshop.Inventory.Dtos;
using Eshop.Inventory.Services;
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
        public async Task<IActionResult> CreateInventory(InventoryDto inventoryDto)
        {
            var inventory = await _inventoryService.CreateInvetoryForProduct(inventoryDto);
            return CreatedAtAction(nameof(GetInventoryById), new { id = inventory.Id }, inventory);
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
        public async Task<IActionResult> UpdateInventory(int id, [FromQuery] int count, [FromQuery] bool increased)
        {
            var result = await _inventoryService.UpdateInventory(id, count, increased);
            if (result == false)
            {
                return BadRequest();
            }
            return NoContent();
        }
    }
}

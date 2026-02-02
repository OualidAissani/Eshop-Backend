using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

namespace Eshop.Orders.Controllers
{
    [Route("api/[controller]")]

    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache;

        public OrderController(IOrderService orderService, IHttpContextAccessor httpContextAccessor, IDistributedCache cache)
        {
            _orderService = orderService;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders()
        {
            return Ok(await _orderService.GetAllOrders());
        }
        [HttpGet("GetAllUserOrders")]
        public async Task<IActionResult> GetAllUserOrders()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders=await _orderService.GetAllUserOrderAsync(userId);
            if(orders==null || orders.Count==0)
            {
                return NotFound();
            }
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto order, [FromHeader(Name = "x_Idempotency_Key")] string key)
        {
            if(key== null)
            {
                return BadRequest("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Order:Create";
            var cached=await _cache.GetAsync(cacheKey);
            if(cached != null)
            {
                return CreatedAtAction(nameof(GetOrderById), new { id = JsonSerializer.Deserialize<Order>(cached)?.Id }, JsonSerializer.Deserialize<Order>(cached) ?? null);
            }
            var createdOrder = await _orderService.CreateOrder(order);

            if(createdOrder == null)
            {
                return BadRequest("We having a problem processing your request.");
            }
            await _cache.SetStringAsync(cacheKey,JsonSerializer.Serialize(createdOrder),new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow= TimeSpan.FromMinutes(5)
            });

            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder?.Id }, createdOrder ?? null);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderDto order, [FromHeader(Name = "x_Idempotency_Key")] string key)
        {
            
            return NoContent();
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var deleteResult=await _orderService.DeleteOrder(id);

            if(!deleteResult)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}

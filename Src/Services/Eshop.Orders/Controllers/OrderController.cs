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
    [Authorize]
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

            var cacheKey = $"Orders:{userId}:All";

            var cachedData=await _cache.GetAsync(cacheKey);

            if (cachedData != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Order>>(cachedData));
            }

            var orders=await _orderService.GetAllUserOrderAsync(userId);

            if(orders==null || orders.Count==0)
            {
                return NotFound();
            }

            await _cache.SetStringAsync(cacheKey,JsonSerializer.Serialize(orders),new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow= TimeSpan.FromMinutes(30)
            });

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var cacheKey = $"Order:{id}";

            var cachedData = await _cache.GetAsync(cacheKey);

            if(cachedData != null)
            {
                return Ok(JsonSerializer.Deserialize<Order>(cachedData));
            }

            var order = await _orderService.GetOrderById(id);

            if (order == null)
            {
                return NotFound();
            }

            await _cache.SetStringAsync(cacheKey,JsonSerializer.Serialize(order),new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow= TimeSpan.FromMinutes(30)
            });

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

            var cacheKey = $"Idempotency:Order:Create:{key}";

            var cached=await _cache.GetAsync(cacheKey);

            if(cached != null)
            {
                return CreatedAtAction(nameof(GetOrderById), new { id = JsonSerializer.Deserialize<Order>(cached)?.Id }, JsonSerializer.Deserialize<Order>(cached) ?? null);
            }

            var userId= _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.UserId = userId;
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

        [HttpPost("OrderCart/{cartId}")]
        public async Task<IActionResult> OrderCart(int cartId, [FromHeader(Name ="x_Idempotency_Key")] string key)
        {
            if (key == null)
            {
                return BadRequest("Idempotency Key is required");
            }

            var cacheKey = $"Idempotency:Order:OrderCart";

            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<Order>(cached));
            }
            if (cartId <= 0)
            {
                return BadRequest("Invalid cart ID.");
            }
            var order=await _orderService.OrderCart(cartId);
            if(order == null)
            {
                return BadRequest("We having a problem processing your request.");
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cached), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
            return Ok(order);
        }
    }
}

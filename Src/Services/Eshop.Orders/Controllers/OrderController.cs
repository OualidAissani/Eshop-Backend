using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

namespace Eshop.Orders.Controllers
{
    [ApiController]
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
        public async Task<IActionResult> GetAllOrders( CancellationToken ct)
        {
            return Ok(await _orderService.GetAllOrders(ct));
        }
        [HttpGet("GetAllUserOrders")]
        public async Task<IActionResult> GetAllUserOrders( CancellationToken ct)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cacheKey = $"Orders:{userId}:All";

            var cachedData=await _cache.GetAsync(cacheKey);

            if (cachedData != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Order>>(cachedData));
            }

            var orders=await _orderService.GetAllUserOrderAsync(userId, ct);

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
        public async Task<IActionResult> GetOrderById(int id, CancellationToken ct)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cacheKey = $"Order:{userId}:{id}";

            var cachedData = await _cache.GetAsync(cacheKey);

            if(cachedData != null)
            {
                return Ok(JsonSerializer.Deserialize<Order>(cachedData));
            }

            var order = await _orderService.GetOrderById(id,userId, ct);

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
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto order, [FromHeader(Name = "x-Idempotency-Key")] string key,CancellationToken ct)
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
            try
            {
                var createdOrder = await _orderService.CreateOrder(order,ct);
                await _cache.SetStringAsync(cacheKey,JsonSerializer.Serialize(createdOrder.Value),new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow= TimeSpan.FromMinutes(5)
                });
                await _cache.RemoveAsync($"Orders:{userId}:All");
                return Ok(createdOrder.Value);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[Authorize]
        //[HttpPut]
        //public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderDto order, [FromHeader(Name = "x_Idempotency_Key")] string key,CancellationToken ct)
        //{

        //    return NoContent();
        //}
        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(int id,CancellationToken ct)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checkOrderMatchWithUser=await _orderService.MatchUserWithOrder(id,userId, ct);

            if (checkOrderMatchWithUser.IsFailed || checkOrderMatchWithUser.Value==false)
            {
                return NotFound(checkOrderMatchWithUser.Errors.FirstOrDefault()?.Message);
            }

            var deleteResult=await _orderService.DeleteOrder(id,ct);

            if(deleteResult.IsFailed)
            {
                return NotFound(deleteResult.Errors.FirstOrDefault()?.Message);
            }
            await _cache.RemoveAsync($"Orders:{userId}:All");
            await _cache.RemoveAsync($"Order:{userId}:{id}");

            return NoContent();
        }

        //[HttpPost("OrderCart/{cartId}")]
        //public async Task<IActionResult> OrderCart(int cartId, [FromHeader(Name ="x_Idempotency_Key")] string key)
        //{
        //    if (key == null)
        //    {
        //        return BadRequest("Idempotency Key is required");
        //    }

        //    var cacheKey = $"Idempotency:Order:OrderCart";

        //    var cached = await _cache.GetAsync(cacheKey);
        //    if (cached != null)
        //    {
        //        return Ok(JsonSerializer.Deserialize<Order>(cached));
        //    }
        //    if (cartId <= 0)
        //    {
        //        return BadRequest("Invalid cart ID.");
        //    }
        //    var order=await _orderService.OrderCart(cartId);
        //    if(order == null)
        //    {
        //        return BadRequest("We having a problem processing your request.");
        //    }
        //    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cached), new DistributedCacheEntryOptions
        //    {
        //        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        //    });
        //    return Ok(order);
        //}
    }
}

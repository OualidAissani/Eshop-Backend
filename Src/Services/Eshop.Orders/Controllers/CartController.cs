using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

namespace Eshop.Orders.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly IHttpContextAccessor _contextAccessor; 
        private readonly ICartService _cartService;
        private readonly IDistributedCache _cache;
        public CartController(ICartService cartService, IHttpContextAccessor contextAccessor, IDistributedCache cache)
        {
            _cartService = cartService;
            _contextAccessor = contextAccessor;
            _cache = cache;
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserCart()
        {
            var user = _contextAccessor.HttpContext?.User;

            if (user == null)
            {
                return Unauthorized("User not authenticated.");
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;

            if (userId == null)
            {
                return Unauthorized("User not authenticated.");
            }
            var cachKey= $"Cart:{userId}:Items";
            var cachedData = await _cache.GetAsync(cachKey);
            if (cachedData != null) {
                return Ok(JsonSerializer.Deserialize<Cart>(cachedData));

            }

            var cart = await _cartService.GetCartItemByUserId(userId);

            if(cart==null)
            {
                return NotFound("Cart not found for the user.");
            }
            await _cache.SetStringAsync(cachKey, JsonSerializer.Serialize(cart), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
            return Ok(cart);
        }
        [HttpGet("{cartId}")]
        public async Task<IActionResult> GetCartById(int cartId)
        {
            var cachedItemKey= $"Cart:{cartId}";

            var cachedItem=await _cache.GetAsync(cachedItemKey);

            if (cachedItem != null)
            {
                return Ok(JsonSerializer.Deserialize<CartItem>(cachedItem));
            }

            var cart = await _cartService.GetAllCartItems(cartId);

            if (cart == null)
            {
                return NotFound("Cart not found.");
            }
            await _cache.SetStringAsync(cachedItemKey, JsonSerializer.Serialize(cart), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return Ok(cart);
        }
        [HttpPost]
        public async Task<IActionResult> AddCartItem([FromBody] CartItemDto cartItem,
            [FromHeader(Name = "x_Idempotency_Key")] string key,CancellationToken ct)
        {
            if (key == null)
            {
                return BadRequest("Idempotency Key is required");
            }
            var user = _contextAccessor.HttpContext.User;
            if(user == null)
            {
                return Unauthorized();
            }
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;


            var cachedKey = $"Idempontency:Cart:{userId}:AddCartItem:{key}";

            var cached= await _cache.GetAsync(cachedKey);

            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<CartItem>(cached));
            }

            var addedItem = await _cartService.AddCartItem(cartItem, ct);

            if (addedItem == null)
            {
                return BadRequest("Failed to add item to cart.");
            }
            var cachKey = $"Cart:{userId}:Items";

            await _cache.RemoveAsync(cachKey);

            await _cache.SetStringAsync(cachedKey, JsonSerializer.Serialize(addedItem), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });


            return Ok(addedItem);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteCartItem(int cartItemId,CancellationToken ct)
        {
            var result = await _cartService.DeleteCartItem(cartItemId,ct);

            if (!result)
            {
                return BadRequest("Failed to delete item from cart.");
            }
            return Ok("Item deleted successfully.");
        }
        [HttpPut]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto cartItem,
            [FromHeader(Name = "x_Idempotency_Key")] string key,CancellationToken ct)
        {
            if (key == null)
            {
                return BadRequest("Idempotency Key is required");
            }

            var cachedKey = $"Idempontency:Cart:{cartItem.ProductId}:UpdateCartItem:{key}";

            var cached = await _cache.GetAsync(cachedKey);

            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<CartItem>(cached));
            }

            var updatedItem = await _cartService.UpdateCartItem(cartItem,ct);

            if (updatedItem == null)
            {
                return BadRequest("Failed to update cart item.");
            }

            await _cache.SetStringAsync(cachedKey, JsonSerializer.Serialize(updatedItem), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            return Ok(updatedItem);
        }



        [HttpDelete("clear/{cartId}")]
        public async Task<IActionResult> ClearCart(int cartId,CancellationToken ct)
        {
            var result = await _cartService.ClearCart(cartId, ct);

            if (!result)
            {
                return BadRequest("Failed to clear cart.");
            }

            return Ok("Cart cleared successfully.");
        }

    }
}

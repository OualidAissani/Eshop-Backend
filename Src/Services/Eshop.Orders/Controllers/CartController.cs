using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eshop.Orders.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly IHttpContextAccessor _contextAccessor; 
        private readonly ICartService _cartService;
        public CartController(ICartService cartService, IHttpContextAccessor contextAccessor)
        {
            _cartService = cartService;
            _contextAccessor = contextAccessor;
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
            var cart = await _cartService.GetCartItemByUserId(userId);
            if(cart==null)
            {
                return NotFound("Cart not found for the user.");
            }
            return Ok(cart);
        }
        [HttpGet("{cartId}")]
        public async Task<IActionResult> GetCartById(int cartId)
        {
            var cart = await _cartService.GetAllCartItems(cartId);
            if (cart == null)
            {
                return NotFound("Cart not found.");
            }
            return Ok(cart);
        }
        [HttpPost]
        public async Task<IActionResult> AddCartItem([FromBody] CartItemDto cartItem)
        {
            var addedItem = await _cartService.AddCartItem(cartItem);
            if (addedItem == null)
            {
                return BadRequest("Failed to add item to cart.");
            }
            return Ok(addedItem);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteCartItem(int cartItemId)
        {
            var result = await _cartService.DeleteCartItem(cartItemId);
            if (!result)
            {
                return BadRequest("Failed to delete item from cart.");
            }
            return Ok("Item deleted successfully.");
        }
        [HttpPut]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto cartItem)
        {
            var updatedItem = await _cartService.UpdateCartItem(cartItem);
            if (updatedItem == null)
            {
                return BadRequest("Failed to update cart item.");
            }
            return Ok(updatedItem);
        }



        [HttpDelete("clear/{cartId}")]
        public async Task<IActionResult> ClearCart(int cartId)
        {
            var result = await _cartService.ClearCart(cartId);
            if (!result)
            {
                return BadRequest("Failed to clear cart.");
            }
            return Ok("Cart cleared successfully.");
        }

    }
}

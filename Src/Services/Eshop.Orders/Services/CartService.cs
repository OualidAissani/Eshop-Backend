using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Eshop.Orders.Services
{
    public class CartService : ICartService
    {
        private readonly OrderDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(OrderDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
           
        }
        private string? GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        }

        public async Task<CartItem> AddCartItem(CartItemDto cartItem)
        {
            if (cartItem == null)
            {
                return null;
            }
            var cartItemEntity = new CartItem()
            {
                ProductId = cartItem.ProductId,
                ProductName = cartItem.ProductName,
                Quantity = cartItem.Quantity,
                FullPrice = cartItem.FullPrice,
                CartId = cartItem.CartId
            };
            if (cartItemEntity.CartId != 0)
            {
                var IsValidCartForUser=_context.Carts.Any(i=>i.Id== cartItemEntity.CartId && i.UserId== GetUserId());
                if (!IsValidCartForUser)
                {
                    return null;
                }
                _context.CartItems.Add(cartItemEntity);
            }
            else
            {
                var cart = new Cart()
                {
                    CartItems = new List<CartItem>() { cartItemEntity },
                    UserId= GetUserId()
                };
                _context.Carts.Add(cart);

            }
            if(await _context.SaveChangesAsync()<=0)
            {
                return null;
            }
            return cartItemEntity;
        }

        public async Task<bool> ClearCart(int cartId)
        {
            var cart = await _context.Carts
                .FirstOrDefaultAsync(i => i.Id==cartId && i.UserId== GetUserId());

            _context.Remove(cart);

            if (await _context.SaveChangesAsync() <= 0)
            {
                return false;
            }
            return true;

        }

        public async Task<bool> DeleteCartItem(int cartItemId)
        {

            if (cartItemId == 0)
            {
                return false;
            }

            var cartItem= await _context.CartItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId== GetUserId());

            _context.CartItems.Remove(cartItem);

            if(await _context.SaveChangesAsync() <= 0)
            {
                return false;
            }
            return true;
        }

        public async Task<List<CartItem>> GetAllCartItems(int cartId)
        {
           var cartitems=await _context.CartItems
                .AsNoTracking()
                .Where(ci => ci.CartId == cartId && ci.Cart.UserId== GetUserId())
                .ToListAsync();

            return cartitems;
        }

        public async Task<CartItem?> GetCartItemById(int cartItemId)
        {
            return await _context.CartItems
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
        }
        public async Task<Cart?> GetCartItemByUserId(string userId)
        {
            return await _context.Carts
                .Include(ci=>ci.CartItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.UserId.Equals(userId));
        }
        public async Task<CartItem> UpdateCartItem(UpdateCartItemDto cartItem)
        {
            if (cartItem == null)
            {
                return null;
            }
            var CartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.ProductId ==cartItem.ProductId);
            CartItem.ProductName = cartItem.ProductName ?? CartItem.ProductName;
            CartItem.Quantity = cartItem.Quantity ?? CartItem.Quantity;
            CartItem.FullPrice = cartItem.FullPrice ?? CartItem.FullPrice;
            if(await _context.SaveChangesAsync() <= 0)
            {
                return null;
            }
            return CartItem;
        }
    }
}

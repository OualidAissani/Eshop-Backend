using Eshop.Orders.Models;

namespace Eshop.Orders.Services.IServices
{
    public interface ICartService
    {
        Task<List<CartItem>> GetAllCartItems(int cartId);

        Task<CartItem?> GetCartItemById(int cartItemId);

        Task<CartItem> AddCartItem(CartItemDto cartItem);

        Task<CartItem> UpdateCartItem(UpdateCartItemDto cartItem);

        Task<Cart?> GetCartItemByUserId(string userId);

        Task<bool> DeleteCartItem(int cartItemId);

        Task<bool> ClearCart(int cartId);
    }
}

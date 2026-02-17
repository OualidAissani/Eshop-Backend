using Eshop.Orders.Models;

namespace Eshop.Orders.Services.IServices
{
    public interface ICartService
    {
        Task<List<CartItem>> GetAllCartItems(int cartId);

        Task<CartItem?> GetCartItemById(int cartItemId);

        Task<CartItem> AddCartItem(CartItemDto cartItem, CancellationToken ct);

        Task<CartItem> UpdateCartItem(UpdateCartItemDto cartItem, CancellationToken ct);

        Task<Cart?> GetCartItemByUserId(string userId);

        Task<bool> DeleteCartItem(int cartItemId, CancellationToken ct);

        Task<bool> ClearCart(int cartId, CancellationToken ct);
    }
}

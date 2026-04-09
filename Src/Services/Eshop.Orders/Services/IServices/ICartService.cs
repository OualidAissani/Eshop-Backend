using Eshop.Orders.Models;
using FluentResults;

namespace Eshop.Orders.Services.IServices
{
    public interface ICartService
    {
        Task<Result<List<CartItem>>> GetAllCartItems(int cartId,CancellationToken ct);

        Task<Result<CartItem?>> GetCartItemById(int cartItemId, CancellationToken ct);

        Task<Result<CartItem>> AddCartItem(CartItemDto cartItem, CancellationToken ct);

        Task<Result<CartItem>> UpdateCartItem(UpdateCartItemDto cartItem, CancellationToken ct);

        Task<Result<Cart?>> GetCartItemByUserId(string userId, CancellationToken ct);

        Task<Result<bool>> DeleteCartItem(int cartItemId, CancellationToken ct);
        Task<Result<bool>> DeleteCartItemByProductId(int productId, CancellationToken ct);

        Task<Result<bool>> ClearCart(int cartId, CancellationToken ct);
    }
}

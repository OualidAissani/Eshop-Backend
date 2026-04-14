using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Eshop.Orders.Services;

public class CartService : ICartService
{
    private readonly OrderDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRequestClient<VerifyProductExistence> _client;
    private readonly IRequestClient<ProductStockRequest> _stockClient;

    public CartService(OrderDbContext context, IHttpContextAccessor httpContextAccessor, IRequestClient<VerifyProductExistence> client, IRequestClient<ProductStockRequest> stockClient)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _client = client;
        _stockClient = stockClient;
    }
    private string? GetUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
    }
    
    public async Task<Result<CartItem>> AddCartItem(CartItemDto cartItem,CancellationToken ct)
    {
        (bool IsSuccessValidation, Result<CartItem> ResultError) = await ProductValidations(cartItem);
        if (!IsSuccessValidation)
        {
            return ResultError;
        }

        var cart = await _context.Carts.Include(i => i.CartItems).FirstOrDefaultAsync(c => c.UserId == GetUserId(), ct);

        if (cart != null)
        {
            (bool IsSuccessAdd, Result<CartItem> AddErrorResult) = await AddItemToExistingUserCart(cartItem, cart, ct);
            if (!IsSuccessAdd)
            {
                return AddErrorResult;
            }

            return cart.CartItems.FirstOrDefault(i => i.ProductId == cartItem.ProductId);
        }
        (bool IsSuccessCreate, Result<CartItem> CreateErrorResult) = await MakeANewCartIncludingTheItem(cartItem, ct);
        if (!IsSuccessCreate)
        {
            return CreateErrorResult;
        }

        return await _context.CartItems.FirstOrDefaultAsync(i => i.ProductId == cartItem.ProductId);
    }

    public async Task<Result<bool>> ClearCart(int cartId,CancellationToken ct)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(i => i.Id==cartId && i.UserId== GetUserId());
        if (cart == null)
        {
            return true;
        }
        _context.Remove(cart);

        if (await _context.SaveChangesAsync(ct) <= 0)
        {
            return false;
        }
        return true;

    }

    public async Task<Result<bool>> DeleteCartItem(int cartItemId,CancellationToken ct)
    {

        if (cartItemId == 0)
        {
            return false;
        }

        var cartItem= await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId== GetUserId());
        if(cartItem == null)
        {
            return false;
        }
        _context.CartItems.Remove(cartItem);

        if(await _context.SaveChangesAsync(ct) <= 0)
        {
            return false;
        }
        return true;
    }

    public async Task<Result<List<CartItem>>> GetAllCartItems(int cartId,CancellationToken ct)
    {
       var cartitems=await _context.CartItems
            .AsNoTracking()
            .Where(ci => ci.CartId == cartId && ci.Cart.UserId== GetUserId())
            .ToListAsync(ct);

        return cartitems;
    }

    public async Task<Result<CartItem?>> GetCartItemById(int cartItemId, CancellationToken ct)
    {
        return await _context.CartItems
            .AsNoTracking()
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId, ct);
    }
    public async Task<Result<Cart?>> GetCartItemByUserId(string userId, CancellationToken ct)
    {
        return await _context.Carts
            .Include(ci=>ci.CartItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(ci => ci.UserId.Equals(userId),ct);
    }
    public async Task<Result<CartItem>> UpdateCartItem(UpdateCartItemDto cartItem,CancellationToken ct)
    {
        if (cartItem == null)
        {
            return Result.Fail<CartItem>("Invalid cart item.");
        }
        var CartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.ProductId ==cartItem.ProductId);

        if (CartItem == null)
        {
            return Result.Fail<CartItem>("Cart item not found.");
        }
        CartItem.ProductName = cartItem.ProductName ?? CartItem.ProductName;
        CartItem.Quantity = cartItem.Quantity ?? CartItem.Quantity;
        CartItem.FullPrice = cartItem.FullPrice ?? CartItem.FullPrice;
        if(await _context.SaveChangesAsync(ct) <= 0)
        {
            return Result.Fail<CartItem>("There was an issue updating the cart item.");
        }
        return CartItem;
    }

    public async Task<Result<bool>> DeleteCartItemByProductId(int productId,CancellationToken ct)
    {
        if(productId <= 0)
        {
            return Result.Fail<bool>("Invalid product id.");
        }
        var cartItem=await _context.CartItems.Where(ci => ci.ProductId == productId).ToListAsync();

        if(cartItem == null || cartItem.Count == 0)
        {
            return Result.Fail<bool>("Cart item not found.");
        }

        _context.RemoveRange(cartItem);

        if(await _context.SaveChangesAsync(ct) == 0)
        {
            return Result.Fail<bool>("There was an issue Deleling The cart item");
        }
        return true;
    }






    private async Task<(bool flowControl, Result<CartItem> value)> MakeANewCartIncludingTheItem(CartItemDto cartItem, CancellationToken ct)
    {
        _context.Carts.Add(new Cart()
        {
            UserId = GetUserId(),
            CartItems = new List<CartItem>()
            {
                new CartItem()
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    Quantity = cartItem.Quantity,
                    FullPrice = cartItem.FullPrice
                }
            }
        });
        if (await _context.SaveChangesAsync(ct) <= 0)
        {
            return (flowControl: false, value: Result.Fail<CartItem>("There was an issue creating the cart and adding the item."));
        }

        return (flowControl: true, value: null);
    }

    private async Task<(bool flowControl, Result<CartItem> value)> AddItemToExistingUserCart(CartItemDto cartItem, Cart cart, CancellationToken ct)
    {
        cart.CartItems.Add(new CartItem()
        {
            ProductId = cartItem.ProductId,
            ProductName = cartItem.ProductName,
            Quantity = cartItem.Quantity,
            FullPrice = cartItem.FullPrice
        });

        if (await _context.SaveChangesAsync(ct) <= 0)
        {
            return (flowControl: false, value: Result.Fail<CartItem>("There was an issue adding the item to the cart."));
        }

        return (flowControl: true, value: null);
    }

    private async Task<(bool flowControl, Result<CartItem> value)> ProductValidations(CartItemDto cartItem)
    {
        if (cartItem == null || cartItem.Quantity <= 0 || cartItem.FullPrice <= 0)
        {
            return (flowControl: false, value: null);
        }
        var ProductExist = await _client.GetResponse<ProductExistenceResponse>(new VerifyProductExistence(cartItem.ProductId));

        if (ProductExist.Message.Exists == false)
        {
            return (flowControl: false, value: Result.Fail<CartItem>("Product does not exist."));
        }

        var StockAvailable = await _stockClient.GetResponse<ProductStockResponse>(new ProductStockRequest(cartItem.ProductId, cartItem.Quantity));

        if (StockAvailable.Message.HasEnoughStock == false)
        {
            return (flowControl: false, value: Result.Fail<CartItem>("Not enough stock available."));
        }

        return (flowControl: true, value: null);
    }

}

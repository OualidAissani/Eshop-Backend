using Eshop.Web.Models;

namespace Eshop.Web.Services;

public class CartService
{
    private readonly List<CartItem> _cartItems = new();

public event Action? OnChange;

    public Task AddToCartAsync(Product product, int quantity = 1)
    {
        var existingItem = _cartItems.FirstOrDefault(item => item.Product.Id == product.Id);
        
    if (existingItem != null)
   {
            existingItem.Quantity += quantity;
        }
        else
  {
    _cartItems.Add(new CartItem { Product = product, Quantity = quantity });
   }

        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public Task RemoveFromCartAsync(int productId)
    {
        var item = _cartItems.FirstOrDefault(item => item.Product.Id == productId);
        if (item != null)
        {
            _cartItems.Remove(item);
            OnChange?.Invoke();
 }
        return Task.CompletedTask;
    }

    public Task UpdateQuantityAsync(int productId, int quantity)
    {
        var item = _cartItems.FirstOrDefault(item => item.Product.Id == productId);
        if (item != null)
     {
            if (quantity <= 0)
         {
 _cartItems.Remove(item);
            }
            else
        {
           item.Quantity = quantity;
   }
            OnChange?.Invoke();
        }
        return Task.CompletedTask;
    }

    public Task<List<CartItem>> GetCartItemsAsync()
    {
        return Task.FromResult(_cartItems);
    }

    public Task<int> GetCartCountAsync()
    {
return Task.FromResult(_cartItems.Sum(item => item.Quantity));
    }

    public Task<decimal> GetCartTotalAsync()
    {
        return Task.FromResult(_cartItems.Sum(item => item.Total));
    }

    public Task ClearCartAsync()
    {
        _cartItems.Clear();
        OnChange?.Invoke();
        return Task.CompletedTask;
    }
}

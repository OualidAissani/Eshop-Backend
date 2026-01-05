namespace Eshop.Web.Models;

public class CartItem
{
    public Product Product { get; set; } = new();
    public int Quantity { get; set; }
    public decimal Total => Product.Price * Quantity;
}

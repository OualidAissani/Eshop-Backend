namespace Eshop.Orders.Models
{
    public class CartItemDto
    {
        required public int ProductId { get; set; }

        required public string ProductName { get; set; }

        required public int Quantity { get; set; }

        required public double FullPrice { get; set; }

        public int CartId { get; set; }
    }
}

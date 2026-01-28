namespace Eshop.Orders.Models
{
    public class UpdateCartItemDto
    {
        required public int ProductId { get; set; }

        public string? ProductName { get; set; }

        public int? Quantity { get; set; }

        public double? FullPrice { get; set; }

    }
}

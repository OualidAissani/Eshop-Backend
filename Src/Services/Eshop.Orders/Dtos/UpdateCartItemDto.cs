namespace Eshop.Orders.Dtos
{
    public class UpdateCartItemDto
    {
        required public int ProductId { get; set; }

        public string? ProductName { get; set; }

        public int? Quantity { get; set; }

        public decimal? FullPrice { get; set; }

    }
}

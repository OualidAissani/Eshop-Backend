using System.ComponentModel.DataAnnotations;

namespace Eshop.Orders.Dtos
{
    public class CartItemDto
    {
        required public int ProductId { get; set; }

        [MinLength(3)]
        required public string ProductName { get; set; }

        required public int Quantity { get; set; } = 1;

        required public decimal FullPrice { get; set; }

        public int CartId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Eshop.Orders.Models
{
    public class CartItemDto
    {
        required public int ProductId { get; set; }

        [MinLength(3)]
        required public string ProductName { get; set; }

        required public int Quantity { get; set; } = 1;

        required public double FullPrice { get; set; }

        public int CartId { get; set; }
    }
}

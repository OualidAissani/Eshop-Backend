using Eshop.Orders.Models;

namespace Eshop.Orders.Dtos
{
    public class CartDto
    {
        required public List<CartItem> CartItems { get; set; }

        public string UserId { get; set; }
    }
}

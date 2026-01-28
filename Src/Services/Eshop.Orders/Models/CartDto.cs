namespace Eshop.Orders.Models
{
    public class CartDto
    {
        required public List<CartItem> CartItems { get; set; }

        public string UserId { get; set; }
    }
}

namespace Eshop.Orders.Models
{
    public class OrderDto
    {
        required public List<OrderItemDto> Products { get; set; }

        public string PayementMethod { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string ShippingAddress { get; set; }
        public string Wilaya { get; set; }
        public string Commune { get; set; }
        public string? UserId { get; set; }
    }

}

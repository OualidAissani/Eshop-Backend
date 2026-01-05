namespace Eshop.Orders.Models
{
    public class OrderDto
    {
        public List<OrderItemDto> Products { get; set; }
        public string UserId { get; set; }
    }
public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

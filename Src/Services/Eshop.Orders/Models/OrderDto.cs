namespace Eshop.Orders.Models
{
    public class OrderDto
    {
        required public List<OrderItemDto> Products { get; set; }

        // client user id
        required public string UserId { get; set; }
    }

}

namespace Eshop.Orders.Models
{
    public class CreateOrderResponseDto
    {
        public Order Order { get; init; } = null!;
        public string? PaymentUrl { get; init; }
    }
}

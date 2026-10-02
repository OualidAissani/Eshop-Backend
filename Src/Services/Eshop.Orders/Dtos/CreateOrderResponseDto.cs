using Eshop.Orders.Entities;

namespace Eshop.Orders.Dtos
{
    public class CreateOrderResponseDto
    {
        public Order Order { get; init; } = null!;
    }
}

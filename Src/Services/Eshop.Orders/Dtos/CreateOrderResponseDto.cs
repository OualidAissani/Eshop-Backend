using Eshop.Orders.Entities;

namespace Eshop.Orders.Dtos
{
    public class CreateOrderResponseDto
    {
        public OrderResponseDto Order { get; init; } = null!;
    }
}

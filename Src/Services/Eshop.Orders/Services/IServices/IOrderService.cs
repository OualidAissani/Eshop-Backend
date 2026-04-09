using Eshop.Orders.Models;

namespace Eshop.Orders.Services.IServices
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrders(CancellationToken ct);

        Task<List<Order>> GetAllUserOrderAsync(string userId, CancellationToken ct);

        Task<Order?> GetOrderById(int orderId, string userId, CancellationToken ct);

        Task<CreateOrderResponseDto> CreateOrder(OrderDto order, CancellationToken ct);

     //  Task<Order> OrderCart(int cartId,CancellationToken ct);

        Task<Order> UpdateOrder(OrderDto order,CancellationToken ct);

        Task<bool> DeleteOrder(int orderId, CancellationToken ct);

    }
}

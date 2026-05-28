using Eshop.Orders.Models;
using FluentResults;

namespace Eshop.Orders.Services.IServices
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrders(CancellationToken ct);

        Task<List<Order>> GetAllUserOrderAsync(string userId, CancellationToken ct);

        Task<Order?> GetOrderById(int orderId, string userId, CancellationToken ct);

        Task<Result<CreateOrderResponseDto>> CreateOrder(OrderDto order, CancellationToken ct);

     //  Task<Order> OrderCart(int cartId,CancellationToken ct);

        Task<Order> UpdateOrder(OrderDto order,CancellationToken ct);

        Task<Result<bool>> DeleteOrder(int orderId, CancellationToken ct);

        Task<Result<bool>> OrderConfirmed(int orderId, CancellationToken ct);

        Task<Result<Order>> UpdateOrderStatus(int orderId, Data.Enums.OrderStatus status, CancellationToken ct);

         Task<Result<bool>> MatchUserWithOrder(int orderId,string userId, CancellationToken ct);

    }
}

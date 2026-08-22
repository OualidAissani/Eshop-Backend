using Eshop.Orders.Dtos;
using Eshop.Orders.Models;
using FluentResults;

namespace Eshop.Orders.Services.IServices
{
    public interface IOrderService
    {
        Task<PaginatedResult<Order>> GetAllOrdersPagination(PaginationParams paginationParams, CancellationToken ct);

        Task<List<Order>> GetAllUserOrderAsync(string userId, CancellationToken ct);

        Task<Order?> GetOrderById(int orderId, string userId, CancellationToken ct);

        Task<Result<CreateOrderResponseDto>> CreateOrder(OrderDto order, CancellationToken ct);

        Task<Result<bool>> DeleteOrder(int orderId, CancellationToken ct);

        Task<Result<bool>> OrderConfirmed(int orderId, CancellationToken ct);

        Task<Result<bool>> UpdateOrderStatus(int orderId, Data.Enums.OrderStatus status, CancellationToken ct);

         Task<Result<bool>> MatchUserWithOrder(int orderId,string userId, CancellationToken ct);
        Task<OrderTrackingDto> GetOrderByOrderNumber(string orderNumber, string phoneNumber, CancellationToken ct);

    }
}

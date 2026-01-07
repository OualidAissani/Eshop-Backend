using Eshop.Orders.Models;

namespace Eshop.Orders.Services.IServices
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrders();
        Task<List<Order>> GetAllUserOrderAsync(string userId);
        Task<Order?> GetOrderById(int orderId);
        Task<Order> CreateOrder(OrderDto order);
        Task<bool> DeleteOrder(int orderId);


    }
}

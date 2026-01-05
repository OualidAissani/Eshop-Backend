using Eshop.Orders.Models;

namespace Eshop.Orders.Services.IServices
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrders();
        Task<Order?> GetOrderById(int orderId);
        Task<Order> CreateOrder(OrderDto order);


    }
}

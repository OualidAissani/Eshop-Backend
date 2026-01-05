using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using MassTransit;
using Microsoft.EntityFrameworkCore;
namespace Eshop.Orders.Services
{
    public class OrderService:IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly IRequestClient<GetProductRequest> _client;
        private readonly HttpClient _httpClient;

        public OrderService(OrderDbContext context,IRequestClient<GetProductRequest> client, HttpClient httpClient)
        {
            _context = context;
            _client = client;
            _httpClient = httpClient;
        }
        public async Task<List<Order>> GetAllOrders()
        {
            return _context.Orders.AsNoTracking().ToList();
        }
        public async Task<Order?> GetOrderById(int orderId)
        {
            return await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        }
        public async Task<Order> CreateOrder(OrderDto order)
        {
            try
            {

                var ordereditems = new List<OrderItem>();

                if (order == null)
                {

                }
                foreach (var pd in order.Products)
                {
                    var ProductInventory = await _client.GetResponse<ProductInventoryAvailibityForOrderResponse>(new ProductInventoryAvailibityForOrderRequest(pd.ProductId, pd.Quantity));
                    if (ProductInventory.Message.IsAvailable == true)
                    {
                        var untipriveresponse = await _client.GetResponse<GetProductResponse>(new GetProductRequest(pd.ProductId));
                        ordereditems.Add(new OrderItem
                        {
                            ProductId = pd.ProductId,
                            Quantity = pd.Quantity,
                            UnitPrice = untipriveresponse.Message.UnitPrice,
                            FullPrice = untipriveresponse.Message.UnitPrice * pd.Quantity,
                            InventoryId = ProductInventory.Message.inventoryId
                        });
                    }
                    //notify the user about the unavailability of the product
                }
                var neworder = new Order()
                {
                    OrderItems = ordereditems,
                    UserId = order.UserId,
                    TotalPrice = ordereditems.Select(i => i.FullPrice).Sum()
                };
                _context.Orders.Add(neworder);
                if (await _context.SaveChangesAsync() == 0)
                {
                    return null;
                }
                foreach (var invendeduction in neworder.OrderItems)
                {
                    var response = await _httpClient.PutAsJsonAsync($"https://localhost:7010/api/Inventory/UpdateInventory/{invendeduction.InventoryId}", new
                    {
                        count = invendeduction.Quantity,
                        increased = false
                    });
                    response.EnsureSuccessStatusCode();

                }
                return neworder;
            }
            catch (Exception ex)
            {
                return null;
            }
            }
        public async Task<bool> DeleteOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                _context.Orders.Remove(order);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
    }
}

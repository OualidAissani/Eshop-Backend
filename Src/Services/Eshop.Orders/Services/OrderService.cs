using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace Eshop.Orders.Services
{
    public class OrderService:IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly IRequestClient<GetProductRequest> _client;
        private readonly IRequestClient<ProductInventoryAvailibityForOrderRequest> _client2;

        private readonly HttpClient _httpClient;

        public OrderService(OrderDbContext context,IRequestClient<GetProductRequest> client, HttpClient httpClient, IRequestClient<ProductInventoryAvailibityForOrderRequest> client2)
        {
            _context = context;
            _client = client;
            _httpClient = httpClient;
            _client2 = client2;
        }
        public async Task<List<Order>> GetAllOrders()
        {
            return _context.Orders.AsNoTracking().ToList();
        }
        public async Task<List<Order>> GetAllUserOrderAsync(string userId)
        {
            return await _context.Orders.Where(i => i.UserId == userId).AsNoTracking().ToListAsync();

        }
        public async Task<Order?> GetOrderById(int orderId)
        {
            return await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        }
        public async Task<Order> CreateOrder(OrderDto order)
        {
            var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var ordereditems = new List<OrderItem>();

                if (order == null)
                {
                    throw new NoNullAllowedException(" no data  were provided");
                }

                foreach (var pd in order.Products)
                {
                    var ProductInventory = await _client2.GetResponse<ProductInventoryAvailibityForOrderResponse>(new ProductInventoryAvailibityForOrderRequest(pd.ProductId, pd.Quantity));
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
                    else
                    {
                        //notify the user about the unavailability of the product

                        return null;
                    }
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
                    var response = await _httpClient.PutAsJsonAsync($"https://localhost:7194/api/Inventory/{invendeduction.InventoryId}?count={invendeduction.Quantity}&increased=false", (object?)null, CancellationToken.None);
                    response.EnsureSuccessStatusCode();

                }
                await transaction.CommitAsync();
                return neworder;
            }
            catch (HttpRequestException ex)
            {
                await transaction.RollbackAsync();

                throw new InvalidOperationException($"Failed to update inventory: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                await transaction.RollbackAsync();

                throw new TimeoutException("Inventory update request timed out", ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                throw new InvalidOperationException($"Failed to create order: {ex.Message}", ex);
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

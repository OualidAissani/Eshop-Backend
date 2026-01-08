using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
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
            if (order == null)
            {
                throw new NoNullAllowedException(" no data  were provided");
            }

            var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ordereditems = new List<OrderItem>();

                
                //all ordered products ids
                var productsIds =order.Products.Select(p=>p.ProductId).ToList();
                //retrieveing Product Inventory details 
                    var ProductInventory = await _client2.GetResponse<ProductInventoryAvailibityForOrderResponse>(new ProductInventoryAvailibityForOrderRequest(productsIds));
                    var ProductInventoryMessage=ProductInventory.Message;
                //retrieving products prices
                var ProductsPrices = await _client.GetResponse<GetProductResponse>(new GetProductRequest(productsIds));
                var Prices=ProductsPrices.Message;


                foreach (var product in order.Products)
                {
                    if(ProductInventoryMessage.Items.Any(i=>i.ProductId==product.ProductId && i.Quantity>=product.Quantity))
                    {
                        ordereditems.Add(new OrderItem
                        {
                            ProductId = product.ProductId,
                            Quantity = product.Quantity,
                            UnitPrice = Prices.Product.First(i=>i.Id==product.ProductId).Price,
                            FullPrice = Prices.Product.First(i => i.Id == product.ProductId).Price * product.Quantity,
                            InventoryId = ProductInventoryMessage.Items.First(i=>i.ProductId==product.ProductId).InventoryId
                        });
                    }
                    else
                    {
                        //notify the user about the unavailability of the product
                        Result.Fail("Product is not available in inventory");
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

                    var response = await _httpClient.PutAsJsonAsync($"https://localhost:7194/api/Inventory/UpdatePrice",
                        neworder.OrderItems.Select(i => new { ProductId = i.ProductId, Quantity = i.Quantity }).ToList());
                response.EnsureSuccessStatusCode();

                
                await transaction.CommitAsync();
                return neworder;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                throw new Exception($"Failed to update inventory: {ex.Message}", ex);
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

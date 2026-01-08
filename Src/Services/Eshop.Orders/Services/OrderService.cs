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
                throw new ArgumentNullException(nameof(order));

            var productIds = order.Products.Select(p => p.ProductId).ToList();

            var inventoryResponse = await _client2.GetResponse<ProductInventoryAvailibityForOrderResponse>(
                new ProductInventoryAvailibityForOrderRequest(productIds)
            );

            var pricesResponse = await _client.GetResponse<GetProductResponse>(
                new GetProductRequest(productIds)
            );

            var inventoryDict = inventoryResponse.Message.Items
                .ToDictionary(i => i.ProductId, i => i);

            var pricesDict = pricesResponse.Message.Product
                .ToDictionary(p => p.Id, p => p.Price);

            var unavailable = order.Products
                .Where(p => !inventoryDict.ContainsKey(p.ProductId) ||
                            inventoryDict[p.ProductId].Quantity < p.Quantity)
                .Select(p => p.ProductId)
                .ToList();

            if (unavailable.Any())
            {
                 Result.Fail(
                    $"Products unavailable: {string.Join(", ", unavailable)}"
                );
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                //Build order items with dictionary lookups (O(1) 
                var orderItems = order.Products
                    .Select(p => new OrderItem
                    {
                        ProductId = p.ProductId,
                        Quantity = p.Quantity,
                        UnitPrice = pricesDict[p.ProductId],              
                        FullPrice = pricesDict[p.ProductId] * p.Quantity,
                        InventoryId = inventoryDict[p.ProductId].InventoryId 
                    })
                    .ToList();

                var newOrder = new Order
                {
                    OrderItems = orderItems,
                    UserId = order.UserId,
                    TotalPrice = orderItems.Sum(i => i.FullPrice)
                };

                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();

                // Update inventory
                var inventoryUpdates = orderItems
                    .Select(i => new { ProductId = i.ProductId, Quantity = i.Quantity })
                    .ToList();

                var response = await _httpClient.PutAsJsonAsync(
                    "https://localhost:7194/api/Inventory/UpdatePrice",
                    inventoryUpdates
                );
                response.EnsureSuccessStatusCode();

                await transaction.CommitAsync();
                return newOrder;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"Order creation failed: {ex.Message}", ex);
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

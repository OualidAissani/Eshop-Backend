using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace Eshop.Orders.Services
{
    public class OrderService:IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly IRequestClient<GetProductRequest> _client;
        private readonly IRequestClient<ProductInventoryAvailibityForOrderRequest> _client2;
        private readonly IUpdateInventory _updateInventory;

        private readonly HttpClient _httpClient;

        public OrderService(OrderDbContext context,IRequestClient<GetProductRequest> client,
            HttpClient httpClient,
            IRequestClient<ProductInventoryAvailibityForOrderRequest> client2,IUpdateInventory updateInventory)
        {
            _context = context;
            _client = client;
            _httpClient = httpClient;
            _client2 = client2;
            _updateInventory = updateInventory;
        }
        public async Task<List<Order>> GetAllOrders()
        {
            return _context.Orders.AsNoTracking().ToList();
        }
        public async Task<List<Order>> GetAllUserOrderAsync(string userId)
        {
            return await _context
                .Orders
                .Where(i => i.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

        }
        public async Task<Order?> GetOrderById(int orderId)
        {
            return await _context
                .Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }
        public async Task<Order> CreateOrder(OrderDto order)
        {
            (Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, double> pricesDict) = await OrderValidations(order);

            try
            {
                (List<OrderItem> orderItems, Order newOrder) = await SaveOrders(order, inventoryDict, pricesDict);

                await ReductOrderedInventory(orderItems);

                return newOrder;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Order creation failed: {ex.Message}", ex);
            }
        }

        private async Task<(List<OrderItem> orderItems, Order newOrder)> SaveOrders(OrderDto order, Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, double> pricesDict)
        {
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
            return (orderItems, newOrder);
        }

        private async Task ReductOrderedInventory(List<OrderItem> orderItems)
        {
            // Update inventory
            var inventoryUpdates = orderItems
                .Select(i => new InventoryUpdateDto { ProductId = i.ProductId, Quantity = i.Quantity })
                .ToList();

            var response = await _updateInventory.UpdateInventory(inventoryUpdates);
            await response.EnsureSuccessStatusCodeAsync();
        }

        private async Task<(Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, double> pricesDict)> OrderValidations(OrderDto order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            if(order.Products == null || !order.Products.Any())
                throw new ArgumentException("Order must contain at least one product.");

            var productIds = order.Products.Select(p => p.ProductId).ToList();

            var inventoryResponse = await _client2.GetResponse<ProductInventoryAvailibityForOrderResponse>(
         new ProductInventoryAvailibityForOrderRequest(productIds));

            var pricesResponse = await _client.GetResponse<GetProductResponse>(
                new GetProductRequest(productIds));

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
                throw new Exception($"Products unavailable: {string.Join(", ", unavailable)}");
            }

            return (inventoryDict, pricesDict);
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

        public Task<Order> UpdateOrder(OrderDto order)
        {
            if(order == null || order.Products == null || !order.Products.Any())
            {
                return null;
            }

            throw new NotImplementedException();
        }
    }
}

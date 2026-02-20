using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net.Http.Headers;
using System.Security.Claims;
namespace Eshop.Orders.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly IRequestClient<GetProductRequest> _client;
        private readonly IRequestClient<ProductInventoryAvailibityForOrderRequest> _client2;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClient;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderService(OrderDbContext context, IRequestClient<GetProductRequest> client,
            IHttpClientFactory httpClient,
            IRequestClient<ProductInventoryAvailibityForOrderRequest> client2, IHttpContextAccessor httpContextAccessor, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _client = client;
            _httpClient = httpClient;
            _client2 = client2;
            _httpContextAccessor = httpContextAccessor;
            _publishEndpoint = publishEndpoint;
        }
        public async Task<List<Order>> GetAllOrders()
        {
            return await _context.Orders.AsNoTracking().ToListAsync();
        }
        public async Task<List<Order>> GetAllUserOrderAsync(string userId)
        {
            return await _context
                .Orders
                .Where(i => i.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

        }
        public async Task<Order?> GetOrderById(int orderId,string userId)
        {
            return await _context
                .Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId==user);
        }

        public async Task<Order> CreateOrder(OrderDto order,CancellationToken ct)
        {

            (Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, decimal> pricesDict) = await OrderValidations(order,ct);

            var orderItems = order.Products
                                .Select(p => new OrderItem
                                {
                                    ProductId = p.ProductId,
                                    Quantity = p.Quantity,
                                    ProductName = "placeholder",
                                    UnitPrice = pricesDict[p.ProductId],
                                    FullPrice = pricesDict[p.ProductId] * p.Quantity,
                                    InventoryId = inventoryDict[p.ProductId].InventoryId
                                })
                                .ToList();

            var newOrder = new Order
            {
                OrderItems = orderItems,
                UserId = order.UserId,
                ShippingAddress = "placeholder",
                ShippedAt = DateTime.UtcNow.AddDays(1),//PLACEHOLDER
                DeliveredAt = DateTime.UtcNow.AddMonths(1),//PLACEHOLDER
                PayementMethod = Data.Enums.PayementMethods.CashOnDelivery,
                TotalPrice = orderItems.Sum(i => i.FullPrice)
            };
            var inventoryParameter = order.Products.Select(s => new InventoryDto
            {
                ProductId = s.ProductId,
                Quantity = s.Quantity
            }).ToList();

            _context.Orders.Add(newOrder);
            if (await _context.SaveChangesAsync() == 0)
            {
                return null;
            }
            else
            {


                await _publishEndpoint.Publish(new OrderSubmitted { OrderId = newOrder.Id, CorrelationId = Guid.NewGuid(), Total = newOrder.TotalPrice, Email = "test@gmail.com", Products = inventoryParameter });
            }
            return newOrder;

        }


        //public async Task<Order> CreateOrder(OrderDto order)
        //{
        //    (Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, decimal> pricesDict) = await OrderValidations(order);

        //    try
        //    {
        //        (List<OrderItem> orderItems, Order newOrder) = await SaveOrders(order, inventoryDict, pricesDict);

        //        await ReductOrderedInventory(orderItems);

        //        return newOrder;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new InvalidOperationException($"Order creation failed: {ex.Message}", ex);
        //    }
        //}

        //private async Task<(List<OrderItem> orderItems, Order newOrder)> SaveOrders(OrderDto order, Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, decimal> pricesDict)
        //{
        //    var orderItems = order.Products
        //                        .Select(p => new OrderItem
        //                        {
        //                            ProductId = p.ProductId,
        //                            Quantity = p.Quantity,
        //                            ProductName="placeholder", 
        //                            UnitPrice = pricesDict[p.ProductId],
        //                            FullPrice = pricesDict[p.ProductId] * p.Quantity,
        //                            InventoryId = inventoryDict[p.ProductId].InventoryId
        //                        })
        //                        .ToList();

        //    var newOrder = new Order
        //    {
        //        OrderItems = orderItems,
        //        UserId = order.UserId,
        //        OrderNumber= Guid.NewGuid().ToString(),
        //        ShippingAddress="placeholder",
        //        ShippedAt=DateTime.UtcNow.AddDays(1),//PLACEHOLDER
        //        DeliveredAt=DateTime.UtcNow.AddMonths(1),//PLACEHOLDER
        //        PayementMethod=Data.Enums.PayementMethods.CashOnDelivery,
        //        TotalPrice = orderItems.Sum(i => i.FullPrice)
        //    };

        //    _context.Orders.Add(newOrder);
        //    await _context.SaveChangesAsync();
        //    return (orderItems, newOrder);
        //}

        //private async Task ReductOrderedInventory(List<OrderItem> orderItems)
        //{
        //    // Update inventory
        //    var inventoryUpdates = orderItems
        //        .Select(i => new Events.InventoryUpdateDto { ProductId = i.ProductId, Quantity = i.Quantity })
        //        .ToList();

        //    //var request = new HttpRequestMessage(HttpMethod.Put, $"{_configuration["InventoryBaseUrl"]}/UpdateQuantity");
        //    //var request = new HttpRequestMessage(HttpMethod.Put, $"{_configuration["GatewayUrl"]}/api/Inventory/UpdateQuantity");

        //    //var token = this._httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        //    //token = token.Substring("Bearer ".Length).Trim();
        //    //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //    //request.Content = JsonContent.Create(inventoryUpdates);
        //    //var response = await _httpClient.SendAsync(request);
        //    //var responseContent=response.Content.ReadAsStringAsync();
        //    //response.EnsureSuccessStatusCode();

        //   await _publishEndpoint.Publish(new ReductInventoryQuantityFromAnOrder ( inventoryUpdates ));

        //}

        private async Task<(Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, decimal> pricesDict)> OrderValidations(OrderDto order,CancellationToken ct)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            if (order.Products == null || !order.Products.Any())
                throw new ArgumentException("Order must contain at least one product.");

            var productIds = order.Products.Select(p => p.ProductId).ToList();
            try
            {
                var inventoryResponse = await _client2.GetResponse<ProductInventoryAvailibityForOrderResponse>(
             new ProductInventoryAvailibityForOrderRequest(productIds), ct);

                var pricesResponse = await _client.GetResponse<GetProductResponse>(
                    new GetProductRequest(productIds), ct);
           
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

                throw new Exception($"Products unavailable: {string.Join(", ", unavailable)}");
            }

            return (inventoryDict, pricesDict);
            }
            catch (MassTransit.RequestTimeoutException ex)
            {
                throw new TimeoutException($"Timeout waiting for inventory/product services. RequestId: {ex.Message} : {ex.Data}", ex);

            }
        }

        public async Task<bool> DeleteOrder(int orderId,CancellationToken ct)
        {
            if(orderId==0)
            {
                return false;
            }
            var user = _httpContextAccessor.HttpContext.User;
            var userId=user.FindFirst(ClaimTypes.NameIdentifier).Value;
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if(order.UserId!= userId)
                {
                    return false;
                }
                _context.Orders.Remove(order);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public Task<Order> UpdateOrder(OrderDto order,CancellationToken ct)
        {
            if (order == null || order.Products == null || !order.Products.Any())
            {
                return null;
            }

            throw new NotImplementedException();
        }

        //public async Task<Order> OrderCart(int cartId,CancellationToken ct)
        //{
        //    if (cartId <= 0)
        //    {
        //        throw new ArgumentException("Invalid cart ID.");
        //    }
        //    var cartitem = await _cartService.GetAllCartItems(cartId);
        //    if (cartitem == null)
        //    {
        //        return null;
        //    }
        //    var orderDto = new OrderDto
        //    {
        //        UserId = cartitem.First().Cart.UserId,
        //        Products = cartitem.Select(ci => new OrderItemDto
        //        {
        //            ProductId = ci.ProductId,
        //            Quantity = ci.Quantity
        //        }).ToList()
        //    };
        //    (Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, decimal> pricesDict) = await OrderValidations(orderDto);
        //    try
        //    {
        //        (List<OrderItem> orderItems, Order newOrder) = await SaveOrders(orderDto, inventoryDict, pricesDict);
        //        await ReductOrderedInventory(orderItems);
        //        await _cartService.ClearCart(cartId,ct);
        //        return newOrder;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new InvalidOperationException($"Order creation failed: {ex.Message}", ex);

        //    }
        //}
    }
}

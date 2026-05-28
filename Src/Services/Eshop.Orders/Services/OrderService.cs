using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
namespace Eshop.Orders.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly IRequestClient<GetProductRequest> _client;
        private readonly IRequestClient<ProductInventoryAvailibityForOrderRequest> _client2;
        private readonly IRequestClient<CreatePaymentRecordRequest> _createPaymentOrderClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderService(OrderDbContext context, IRequestClient<GetProductRequest> client,
            IRequestClient<ProductInventoryAvailibityForOrderRequest> client2, IHttpContextAccessor httpContextAccessor, IPublishEndpoint publishEndpoint, IRequestClient<CreatePaymentRecordRequest> createPaymentOrderClient)
        {
            _context = context;
            _client = client;
            _client2 = client2;
            _httpContextAccessor = httpContextAccessor;
            _publishEndpoint = publishEndpoint;
            _createPaymentOrderClient = createPaymentOrderClient;
        }
        public async Task<List<Order>> GetAllOrders( CancellationToken ct)
        {
            return await _context.Orders.Include(o => o.OrderItems).AsSplitQuery().AsNoTracking().ToListAsync(ct);
        }
        public async Task<List<Order>> GetAllUserOrderAsync(string userId, CancellationToken ct)
        {
            return await _context
                .Orders
                .Include(o => o.OrderItems)
                .Where(i => i.UserId == userId)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync(ct);

        }
        public async Task<Order?> GetOrderById(int orderId,string userId, CancellationToken ct)
        {
            var order= await _context.Orders
                .Include(i=>i.OrderItems)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);
            return order;
        }

        //refactory is a must
        public async Task<Result<CreateOrderResponseDto>> CreateOrder(OrderDto order,CancellationToken ct)
        {

            (Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, GetProductResponseDto> pricesDict) = await OrderValidations(order,ct);

            var orderItems = order.Products
                                .Select(p => new OrderItem
                                {
                                    ProductId = p.ProductId,
                                    Quantity = p.Quantity,
                                    ProductName = pricesDict[p.ProductId].Name,
                                    UnitPrice = pricesDict[p.ProductId].Price,
                                    FullPrice = pricesDict[p.ProductId].Price * p.Quantity,
                                    InventoryId = inventoryDict[p.ProductId].InventoryId
                                })
                                .ToList();
            if (!Enum.TryParse<Data.Enums.PaymentMethods>(order.PayementMethod, true, out var payementMethodEnum))
            {
                throw new ArgumentException($"Invalid payment method: {order.PayementMethod}");
            }

            var newOrder = new Order
            {
                OrderItems = orderItems,
                UserId = order.UserId,
                ShippingAddress = order.ShippingAddress,
                PayementMethod = payementMethodEnum,
                Email=order.Email,
                TotalPrice = orderItems.Sum(i => i.FullPrice)
            };
            var inventoryParameter = order.Products.Select(s => new Events.InventoryUpdateDto
            {
                ProductId = s.ProductId,
                Quantity = s.Quantity
            }).ToList();

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);

                _context.Orders.Add(newOrder);

                if (await _context.SaveChangesAsync(ct) == 0)
                {
                    throw new Exception("Error Occured While Saving Order To The Db");
                }

                var paymentItems = new List<Events.OrderItemSagaDto>();
                if (newOrder.PayementMethod != Data.Enums.PaymentMethods.CashOnDelivery)
                {
                    paymentItems = orderItems.Select(i => new Events.OrderItemSagaDto
                    {
                        name = i.ProductName,
                        quantity = i.Quantity,
                        description = $"Order item for product {i.ProductId}",
                        unit_amount = new AmountDto
                        {
                            value = i.UnitPrice,
                            currency_code = "USD"
                        }

                    }).ToList();
                }
                var PaypalUrl = string.Empty;
                var correlationId = Guid.NewGuid();
                if (payementMethodEnum != Data.Enums.PaymentMethods.CashOnDelivery)
                {
                    var PaymentUrl = await _createPaymentOrderClient.GetResponse<CreatePaymentRecordResponse>(new CreatePaymentRecordRequest
                    {
                        Amount = newOrder.TotalPrice,
                        Items = paymentItems,
                        CorrelationId = correlationId,
                        OrderId = newOrder.Id
                    });
                    PaypalUrl = PaymentUrl.Message.PaymentUrl;
                }
                await _publishEndpoint.Publish(
                new OrderSubmitted
                {
                    OrderId = newOrder.Id,
                    CorrelationId = correlationId,
                    PaymentMethod = (Eshop.Events.PaymentMethods)newOrder.PayementMethod,
                    Total = newOrder.TotalPrice,
                    Email = newOrder.Email,
                    Products = inventoryParameter,
                    PaymentItems = paymentItems ?? new List<Events.OrderItemSagaDto>()
                });

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return new CreateOrderResponseDto
                {
                    Order = newOrder,
                    PaymentUrl = PaypalUrl ?? ""
                };

            });
        }



        public async Task<Result<bool>> DeleteOrder(int orderId,CancellationToken ct)
        {
            if(orderId<=0)
            {
                return Result.Fail<bool>("Invalid order ID.");
            }
           
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
                if(order == null)
                {
                    return Result.Fail<bool>("Order not found.");
                }
                _context.Orders.Remove(order);

                if(await _context.SaveChangesAsync(ct) == 0)
                {
                    return Result.Fail<bool>($"Failed to delete order {orderId}.");
                }
                return true;
            }
            catch (Exception ex)
            {
                return Result.Fail<bool>($"Failed to delete order {orderId}: {ex.Message}");
            }
        }

        public Task<Order> UpdateOrder(OrderDto order,CancellationToken ct)
        {
            if (order == null || order.Products == null || !order.Products.Any())
            {
                throw new ArgumentNullException(nameof(order), "Order and its products cannot be null or empty.");
            }

            throw new NotImplementedException();
        }

        public async Task<Result<bool>> OrderConfirmed(int orderId, CancellationToken ct)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId,ct);
            if(order == null)
            {
                return Result.Fail<bool>("Order not found.");
            }
            order.Status = Data.Enums.OrderStatus.Confirmed;

            if(await _context.SaveChangesAsync(ct) ==0)
            {
                return Result.Fail<bool>("Failed to update order status to confirmed.");
            }

            return true;

        }

        public async Task<Result<Order>> UpdateOrderStatus(int orderId, Data.Enums.OrderStatus status, CancellationToken ct)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order == null)
            {
                return Result.Fail<Order>("Order not found.");
            }

            order.Status = status;

            if (status == Data.Enums.OrderStatus.Shipped)
            {
                order.ShippedAt = DateTime.UtcNow;
            }

            if (status == Data.Enums.OrderStatus.Delivered)
            {
                order.DeliveredAt = DateTime.UtcNow;
            }

            if (await _context.SaveChangesAsync(ct) == 0)
            {
                return Result.Fail<Order>("Failed to update order status.");
            }

            return order;
        }

        public async Task<Result<bool>> MatchUserWithOrder(int orderId,string userId, CancellationToken ct)
        {

            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order == null)
            {
                return Result.Fail<bool>("Order not found.");
            }
            return order.UserId == userId;
        }




        private async Task<(Dictionary<int, ProductInventoryItem> inventoryDict, Dictionary<int, GetProductResponseDto> pricesDict)> OrderValidations(OrderDto order, CancellationToken ct)
        {

            ArgumentNullException.ThrowIfNull(order);

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
                    .ToDictionary(p => p.Id, p => p);

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

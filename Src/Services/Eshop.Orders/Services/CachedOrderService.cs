using Eshop.Orders.Data.Enums;
using Eshop.Orders.Dtos;
using Eshop.Orders.Entities;
using Eshop.Orders.Services.IServices;
using FluentResults;
using REDox.Json;
using StackExchange.Redis;

namespace Eshop.Orders.Services
{
    public class CachedOrderService : IOrderService
    {
        private readonly IOrderService _orderService;
        private readonly IDatabase _redisDb;
        public CachedOrderService(IOrderService orderService, IDatabase redisDb)
        {
            _orderService = orderService;
            _redisDb = redisDb;
        }
        public async Task<Result<CreateOrderResponseDto>> CreateOrder(OrderDto order, CancellationToken ct)
        {
            if (order.IdempontencyKey== null)
            {
                return Result.Fail("Idempotency Key is required");
            }

            var key = $"Idempotency:Order:Create:{order.UserId}:{order.IdempontencyKey}";
            var reserved = await _redisDb.StringSetAsync(key, "in-progress", TimeSpan.FromHours(24), When.NotExists);

            if (!reserved)
            {
                var existing = await _redisDb.StringGetAsync(key);
                if (existing == "in-progress")
                    return Result.Fail("Request already in progress"); // or 409
                return JsonSerializer.Deserialize<CreateOrderResponseDto>(existing.ToString())!;
            }

            var createdOrder = await _orderService.CreateOrder(order, ct);
            await _redisDb.StringSetAsync(key, JsonSerializer.Serialize(createdOrder.Value), TimeSpan.FromHours(24));

            return createdOrder.Value;
        }

        public async Task<Result<bool>> DeleteOrder(int orderId,string userId, CancellationToken ct)
        {

            var deleteResult = await _orderService.DeleteOrder(orderId,userId, ct);

            if (deleteResult.IsFailed)
            {
                return Result.Fail(deleteResult.Errors[0].Message);
            }
            await _redisDb.KeyDeleteAsync($"Order:{userId}:{orderId}");

            return true;
        }

        public async Task<PaginatedResult<OrderResponseDto>> GetAllOrdersPagination(PaginationParams paginationParams, CancellationToken ct)
        {


            var orders = await _orderService.GetAllOrdersPagination(paginationParams, ct);

    
            return orders;
        }

        public async Task<List<OrderResponseDto>> GetAllUserOrderAsync(string userId, CancellationToken ct)
        {

            var orders = await _orderService.GetAllUserOrderAsync(userId, ct);

            if (orders == null || orders.Count == 0)
            {
                return null;
            }

           

            return orders;
        }

        public async Task<OrderResponseDto?> GetOrderById(int orderId, string userId, CancellationToken ct)
        {
            var cacheKey = $"Order:{userId}:{orderId}";


                var cachedData = await _redisDb.StringGetAsync(cacheKey);
                if (cachedData.HasValue)
                {
                return JsonSerializer.Deserialize<OrderResponseDto>(cachedData.ToString());

            }

            var order = await _orderService.GetOrderById(orderId, userId, ct);

            if (order is null)
            {
                return null;
            }

            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(order),TimeSpan.FromHours(24));

            return order;
        }

        public async Task<OrderTrackingDto> GetOrderByOrderNumber(string orderNumber, string phoneNumber, CancellationToken ct)
        {
            var cacheKey = $"Order:Tracking:{orderNumber}:{phoneNumber}";

                var cahcedData = await _redisDb.StringGetAsync(cacheKey);
                if (cahcedData.HasValue)
                {
                return JsonSerializer.Deserialize<OrderTrackingDto>(cahcedData.ToString())!;
            }

            var orderTracking = await _orderService.GetOrderByOrderNumber(orderNumber, phoneNumber, ct);

            await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(orderTracking),TimeSpan.FromHours(24));

            return orderTracking;

        }

        public async Task<Result<bool>> MatchUserWithOrder(int orderId, string userId, CancellationToken ct)
        {
            return await _orderService.MatchUserWithOrder(orderId, userId, ct);
        }

        public async  Task<Result<bool>> OrderConfirmed(int orderId, CancellationToken ct)
        {
            return await _orderService.OrderConfirmed(orderId, ct);  
        }

        public async Task<Result<bool>> UpdateOrderStatus(int orderId, OrderStatus status, CancellationToken ct)
        {
            var result = await _orderService.UpdateOrderStatus(orderId, status, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.FirstOrDefault()?.Message);
            }


            return result.Value;
        }
    }
}

using Eshop.Orders.Data.Enums;
using Eshop.Orders.Dtos;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

namespace Eshop.Orders.Services
{
    public class CachedOrderService : IOrderService
    {
        private readonly IOrderService _orderService;
        private readonly IDistributedCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CachedOrderService(IOrderService orderService, IDistributedCache cache, IHttpContextAccessor httpContextAccessor)
        {
            _orderService = orderService;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<Result<CreateOrderResponseDto>> CreateOrder(OrderDto order, CancellationToken ct)
        {
            if (order.IdempontencyKey== null)
            {
                return Result.Fail("Idempotency Key is required");
            }

            var cacheKey = $"Idempotency:Order:Create:{order.IdempontencyKey}";

            var cached = await _cache.GetAsync(cacheKey);

            if (cached != null)
            {
                return JsonSerializer.Deserialize<CreateOrderResponseDto>(cached);
            }

            order.UserId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var createdOrder = await _orderService.CreateOrder(order, ct);
                if (createdOrder.IsFailed)
                {
                    return Result.Fail(createdOrder.Errors[0].Message);
                }
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(createdOrder.Value), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
                await _cache.RemoveAsync($"Orders:{order.UserId}:All");
                return createdOrder.Value;
            }
            catch (ArgumentException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Result<bool>> DeleteOrder(int orderId, CancellationToken ct)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checkOrderMatchWithUser = await _orderService.MatchUserWithOrder(orderId, userId, ct);

            if (checkOrderMatchWithUser.IsFailed || checkOrderMatchWithUser.Value == false)
            {
                return Result.Fail("something went wrong");
            }

            var deleteResult = await _orderService.DeleteOrder(orderId, ct);

            if (deleteResult.IsFailed)
            {
                return Result.Fail(deleteResult.Errors[0].Message);
            }
            await _cache.RemoveAsync($"Orders:{userId}:All");
            await _cache.RemoveAsync($"Order:{userId}:{orderId}");

            return true;
        }

        public async Task<PaginatedResult<Order>> GetAllOrdersPagination(PaginationParams paginationParams, CancellationToken ct)
        {


            var orders = await _orderService.GetAllOrdersPagination(paginationParams, ct);

    
            return orders;
        }

        public async Task<List<Order>> GetAllUserOrderAsync(string userId, CancellationToken ct)
        {

            var orders = await _orderService.GetAllUserOrderAsync(userId, ct);

            if (orders == null || orders.Count == 0)
            {
                return null;
            }

           

            return orders;
        }

        public async Task<Order?> GetOrderById(int orderId, string userId, CancellationToken ct)
        {
            var cacheKey = $"Order:{userId}:{orderId}";

            var cachedData = await _cache.GetAsync(cacheKey);

            if (cachedData != null)
            {
                return JsonSerializer.Deserialize<Order>(cachedData);
            }

            var order = await _orderService.GetOrderById(orderId, userId, ct);

            if (order == null)
            {
                return null;
            }

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(order), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return order;
        }

        public async Task<OrderTrackingDto> GetOrderByOrderNumber(string orderNumber, string phoneNumber, CancellationToken ct)
        {
            var cacheKey = $"Order:Tracking:{orderNumber}:{phoneNumber}";
            var cahcedData = await _cache.GetStringAsync(cacheKey);
            if (cahcedData != null)
            {
                return JsonSerializer.Deserialize<OrderTrackingDto>(cahcedData);
            }
            var orderTracking = await _orderService.GetOrderByOrderNumber(orderNumber, phoneNumber, ct);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orderTracking), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
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

            await _cache.RemoveAsync($"Orders:Admin:All");

            return result.Value;
        }
    }
}

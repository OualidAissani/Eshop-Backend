using Eshop.Orders.Dtos;
using Eshop.Orders.Entities;
using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using MassTransit.Internals.GraphValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

namespace Eshop.Orders.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDeliveryPriceService _deliveryPriceService;

        public OrderController(IOrderService orderService, IHttpContextAccessor httpContextAccessor,
            IDeliveryPriceService deliveryPriceService)
        {
            _orderService = orderService;
            _httpContextAccessor = httpContextAccessor;
            _deliveryPriceService = deliveryPriceService;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] PaginationParams paging, CancellationToken ct)
        {
            var orders = await _orderService.GetAllOrdersPagination(paging, ct);
            return Ok(orders);
        }
        [HttpGet("GetAllUserOrders")]
        public async Task<IActionResult> GetAllUserOrders( CancellationToken ct)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            var orders=await _orderService.GetAllUserOrderAsync(userId, ct);

            if(orders==null || orders.Count==0)
            {
                return NotFound();
            }
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id, CancellationToken ct)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);


            var order = await _orderService.GetOrderById(id,userId, ct);

            if (order == null)
            {
                return NotFound();
            }


            return Ok(order);
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateRequest request, CancellationToken ct)
        {
            if (!Enum.TryParse<Data.Enums.OrderStatus>(request.Status, true, out var status))
            {
                return BadRequest("Invalid order status.");
            }

            var result = await _orderService.UpdateOrderStatus(id, status, ct);
            if (result.IsFailed)
            {
                return NotFound(result.Errors.FirstOrDefault()?.Message);
            }

            return Ok(result.Value);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto order,
            [FromHeader(Name = "x-Idempotency-Key")] string key,CancellationToken ct)
        {
            
             if (order.WilayaCode is < 1 or > 58)
            {
                return BadRequest("A valid wilaya code is required.");
            }

            order.UserId= _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.IdempontencyKey = key;
            try
            {
                var createdOrder = await _orderService.CreateOrder(order,ct);
                if (createdOrder.IsFailed)
                {
                    return BadRequest(createdOrder.Errors[0].Message);
                }
               
                return Ok(createdOrder.Value);
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

     
        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(int id,CancellationToken ct)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var deleteResult =await _orderService.DeleteOrder(id,userId,ct);

            if(deleteResult.IsFailed)
            {
                return NotFound(deleteResult.Errors.FirstOrDefault()?.Message);
            }
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("trackOrder")]
        public async Task<IActionResult> TrackOrder([FromQuery] string code, [FromQuery] string phone,CancellationToken ct)
        {
            return Ok(await _orderService.GetOrderByOrderNumber(code, phone, ct));
        }

        /// <summary>
        /// Returns the delivery price for a destination wilaya and delivery type.
        /// The chart is maintained by the shop owner from the delivery company's grid.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("delivery-price")]
        public async Task<IActionResult> GetDeliveryPrice([FromQuery] int wilayaCode,
            [FromQuery] Data.Enums.DeliveryType deliveryType, CancellationToken ct)
        {
            var price = await _deliveryPriceService.GetPriceAsync(wilayaCode, deliveryType, ct);
            if (price is null)
            {
                return NotFound(new
                {
                    message = $"No delivery price configured for wilaya {wilayaCode} ({deliveryType})."
                });
            }

            return Ok(new
            {
                wilayaCode,
                deliveryType,
                priceDzd = price.Value
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("delivery-prices")]
        public async Task<IActionResult> GetDeliveryPrices(CancellationToken ct)
        {
            return Ok(await _deliveryPriceService.GetAllAsync(ct));
        }

        /// <summary>
        /// Bulk insert/update the delivery chart. Send the full grid once to load it.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("delivery-prices")]
        public async Task<IActionResult> UpsertDeliveryPrices([FromBody] List<DeliveryPriceDto> prices,
            CancellationToken ct)
        {
            try
            {
                await _deliveryPriceService.UpsertAsync(prices, ct);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

using Eshop.Orders.Dtos;
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

        public OrderController(IOrderService orderService, IHttpContextAccessor httpContextAccessor)
        {
            _orderService = orderService;
            _httpContextAccessor = httpContextAccessor;
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
            var deleteResult=await _orderService.DeleteOrder(id,ct);

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
      
    }
}

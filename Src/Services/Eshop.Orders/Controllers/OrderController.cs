using Eshop.Orders.Models;
using Eshop.Orders.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eshop.Orders.Controllers
{
    [Route("api/[controller]")]

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
        public async Task<IActionResult> GetAllOrders()
        {
            return Ok(await _orderService.GetAllOrders());
        }
        [HttpGet("GetAllUserOrders")]
        public async Task<IActionResult> GetAllUserOrders()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders=await _orderService.GetAllUserOrderAsync(userId);
            if(orders==null || orders.Count==0)
            {
                return NotFound();
            }
            return Ok(orders);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto order)
        {
            var createdOrder = await _orderService.CreateOrder(order);
            if(createdOrder==null)
            {
                return BadRequest();
            }
            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder?.Id }, createdOrder?? null);
        }
        [HttpPut()]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderDto order)
        {

            return NoContent();
        }
        [HttpDelete()]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var delete=await _orderService.DeleteOrder(id);
            if(delete==false)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}

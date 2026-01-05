using Microsoft.AspNetCore.Mvc;

namespace Eshop.Orders.Controllers
{
    [Route("api/[controller]")]

    public class OrderController : ControllerBase
    {
        public IActionResult Index()
        {
            return Ok();
        }
    }
}

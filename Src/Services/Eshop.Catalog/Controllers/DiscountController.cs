using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.Catalog.Controllers;

public class DiscountController:ControllerBase
{
    private readonly MongoCatalogContext _context;
    private readonly IDiscountService _discountService;
    public DiscountController(MongoCatalogContext context, IDiscountService discountService)
    {
        _context = context;
        _discountService = discountService;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddDiscount([FromBody] DiscountDto discount, CancellationToken ct)
    {
        var discountResult = await _discountService.AddDiscount(discount, ct);
        if (discountResult.IsFailed)
        {
            return BadRequest(discountResult.Errors);

        }

        return Ok(discountResult.Value);
    }


    [HttpGet]
    public async Task<IActionResult> GetDiscounts(CancellationToken ct)
    {
        var discounts = await _discountService.GetDiscounts(ct);
        return Ok(discounts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDiscountByProductId(int id, CancellationToken ct)
    {
        var discount = await _discountService.GetDiscountByProductId(id, ct);
        if (discount.IsFailed)
        {
            return BadRequest(discount.Errors);
        }
        return Ok(discount.Value);
    }



}

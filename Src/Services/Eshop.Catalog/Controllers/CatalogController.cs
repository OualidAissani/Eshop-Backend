using Eshop.Catalog.Dtos;
using Eshop.Catalog.Entities;
using Eshop.Catalog.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.IO;
using System.Text.Json;

namespace Eshop.Catalog.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CatalogController : ControllerBase
{
    private readonly IProductService _productrepo;

    public CatalogController(
        IProductService productRepository
)
    {
        _productrepo = productRepository;
    }

    [RequestSizeLimit(50_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto product,
    [FromForm(Name = "formFile")] List<IFormFile> formFile,
    [FromHeader(Name = "x-Idempotency-Key")] string key, CancellationToken ct)
    {
        if(product == null)
        {
            return BadRequest("Product Data Is Required");
        }
        if (formFile == null || formFile.Count == 0)
        {
            return BadRequest("Atleast One Image Attached To The Product");
        }
        if (key == null)
        {
            return BadRequest("Idempotency Key is required");   
        }
       
        product.IdempotencyKey = key;
        var result = await _productrepo.CreateProduct(product,formFile,ct); 

        if(result.IsFailed)
        {
            return BadRequest(result.Errors.First().Message);
        }
     
       
        return CreatedAtAction(nameof(GetProductById),new {id=result.Value?.Id},result.Value);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(
        int id,
        [FromForm] ProductsUpdateDto product,
        [FromForm(Name = "formFile")] List<IFormFile>? formFile,
        [FromForm] bool AppendImage,
       CancellationToken ct)
    {

        var result = await _productrepo.UpdateProduct(id,product, formFile, ct, AppendImage);


        if(result.IsFailed)
        {
            return BadRequest(result.Errors.First().Message);
        } 

        return Ok(result.Value);

    }

    [HttpDelete("{Id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int Id,CancellationToken ct)
    {
        var result = await _productrepo.DeleteProductReturnOldProduct(Id,ct);
        if (result.IsFailed)
        {
            return BadRequest(result.Errors.First().Message);
        }
       
        return Ok(new {message="The Product Has Been Deleted Successfully"});
    }
    
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ProductDto>>> GetProducts([FromQuery] int? lastId, CancellationToken ct, [FromQuery] int pageSize = 10)
    {
        //var cacheKey= $"Products:List:PageSize={pageSize}:LastId={lastId}"; //TO FIND BETTER SOLUTIONS LATER
        //var cached = await _cache.GetStringAsync(cacheKey);
        //if (cached != null)
        //{
        //    var cachedResult = JsonSerializer.Deserialize<PaginatedResult<ProductDto>>(cached);
        //    return Ok(cachedResult);
        //}

        var result = await _productrepo.GetProductsAsync(new PaginationParams
        {
            PageSize = pageSize,
            LastId=lastId
        },ct);
        //await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        //{
        //    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        //});
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProductById(int id, CancellationToken ct)
    {
        
        var product = await _productrepo.GetProductById(id, ct);

        if (product == null)
        {
            return NotFound();
        }
        
        return Ok(product);
    }
    
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<List<ProductDto>>> GetProductsByCategory(int categoryId, CancellationToken ct)
    {
       
        var products = await _productrepo.GetProductsByCategory(categoryId,ct);

        
        return Ok(products);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q,CancellationToken ct)
    {
        if (q == null)
        {
            return BadRequest("You To Add A Search Term/Tag");
        }
        

        var products=await _productrepo.ProductSearch(q,ct);
        if (products == null)
        {
            return NotFound();
        }
           

        return Ok(products);
    }
    [HttpGet("hero")]
    public async Task<IActionResult> GetHeroProducts(CancellationToken ct)
    {
        
        var products = await _productrepo.GetHeroProducts(ct);

        return Ok(products);
    }

    [HttpPut("{id}/hero")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateHeroSelection(int id, [FromBody] ProductHeroUpdateDto dto, CancellationToken ct)
    {
        var result = await _productrepo.UpdateHeroSelection(id, dto, ct);

        if (result.IsFailed)
        {
            return BadRequest(result.Errors.First().Message);
        }
        return Ok(result.Value);
    }


}

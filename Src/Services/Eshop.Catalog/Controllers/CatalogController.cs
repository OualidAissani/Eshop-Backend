using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.IO;
using System.Text.Json;

namespace Eshop.Catalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly IProductRepository _productrepo;
        private readonly IMediaService _mediaService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CatalogController> _logger;

        public CatalogController(
            IProductRepository productRepository,
            ILogger<CatalogController> logger,
            IMediaService mediaService
,
            IDistributedCache cache)
        {
            _productrepo = productRepository;
            _logger = logger;
            _mediaService = mediaService;
            _cache = cache;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto product, List<IFormFile>? formFile, [FromHeader(Name = "x_Idempotency_Key")] string key)
        {
            if (key == null)
            {
                return BadRequest("Idempotency Key is required");   
            }
            var cacheKey = $"Idempontency:Product:Create:{key}";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return CreatedAtAction(nameof(GetProductById), new { id = System.Text.Json.JsonSerializer.Deserialize<Products>(cached)?.Id }, System.Text.Json.JsonSerializer.Deserialize<Products>(cached) ?? null);
            }
            if (formFile == null || formFile.Count == 0)
            {

            }
            var result = await _productrepo.CreateProduct(product);
            var media = new ProductMedia()
            {
                ProductId = result.Id,
                Description = result.Description
            };
            foreach (var file in formFile)
            {              
                using var stream = file.OpenReadStream();
                await _mediaService.CreateMedia(media,stream,file.ContentType,file.FileName);

            }
            Products currentProduct = await _productrepo.GetProductById(result.Id);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(currentProduct), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return Ok(currentProduct);
        }
        
        [HttpPost("update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct([FromForm] Products product, IFormFile? formFile , [FromHeader(Name = "x_Idempotency_Key")] string key)
        {
            if (key == null)
            {
                return BadRequest("Idempotency Key is required");
            }
            var cacheKey = $"Idempontency:Product:Update:{key}";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<Products>(cached) ?? null);
            }
            var result=new Products();
            if (formFile != null)
            {
                using var stream = formFile.OpenReadStream();
                result = await _productrepo.UpdateProduct(product, stream, formFile.ContentType, formFile.FileName);
            }
            else
            {
                 result = await _productrepo.UpdateProduct(product, Stream.Null, string.Empty, string.Empty);
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return Ok(result);

        }

        [HttpDelete("{Id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int Id)
        {
            var result = await _productrepo.DeleteProduct(Id);
            if (!result)
            {
                return NotFound();
            }
            return Ok(new {message="The Product Has Been Deleted Successfully"});
        }
        
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<Products>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = User.Identity.Name;
            }
            var result = await _productrepo.GetProductsAsync(new PaginationParams
            {
                Page = page,
                PageSize = pageSize
            });

            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Products>> GetProductById(int id)
        {
            var product = await _productrepo.GetProductById(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<Products>>> GetProductsByCategory(int categoryId)
        {
            var products = await _productrepo.GetProductsByCategory(categoryId);
            return Ok(products);
        }
        

    }
}

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
        private readonly IProductService _productrepo;
        private readonly IMediaService _mediaService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CatalogController> _logger;

        public CatalogController(
            IProductService productRepository,
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
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto product, List<IFormFile>? formFile,
            [FromHeader(Name = "x_Idempotency_Key")] string key,CancellationToken ct)
        {
            if (key == null)
            {
                return BadRequest("Idempotency Key is required");   
            }
            var cacheKey = $"Idempotency:Product:Create:{key}";

            var cached = await _cache.GetAsync(cacheKey);

            if (cached != null)
            {
                return CreatedAtAction(nameof(GetProductById), new { id = JsonSerializer.Deserialize<Products>(cached)?.Id },JsonSerializer.Deserialize<Products>(cached) ?? null);
            }
            if (formFile == null || formFile.Count == 0)
            {

            }
            var result = await _productrepo.CreateProduct(product,ct);

            if(result == null)
            {
                return BadRequest("Product Creation Failed");
            }
            var media = new ProductMedia()
            {
                ProductId = result.Id,
                Description = result.Description
            };

            foreach (var file in formFile)
            {              
                using var stream = file.OpenReadStream();

                await _mediaService.CreateMedia(media,stream,file.ContentType,file.FileName,ct);

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
        public async Task<IActionResult> UpdateProduct([FromForm] Products product, IFormFile? formFile ,
            [FromHeader(Name = "x_Idempotency_Key")] string key,CancellationToken ct)
        {
            if (key == null)
            {
                return BadRequest("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Product:Update:{key}";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<Products>(cached) ?? null);
            }
            var result=new ProductDto();
            if (formFile != null)
            {
                using var stream = formFile.OpenReadStream();
                result = await _productrepo.UpdateProduct(product, stream, formFile.ContentType, formFile.FileName,ct);
            }
            else
            {
                 result = await _productrepo.UpdateProduct(product, Stream.Null, string.Empty, string.Empty,ct);
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return Ok(result);

        }

        [HttpDelete("{Id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int Id,CancellationToken ct)
        {
            var result = await _productrepo.DeleteProduct(Id,ct);
            if (!result)
            {
                return NotFound();
            }
            return Ok(new {message="The Product Has Been Deleted Successfully"});
        }
        
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<Products>>> GetProducts([FromQuery] int? lastId,[FromQuery] int pageSize = 10)
        {
            var cacheKey= $"Products:List:PageSize={pageSize}:LastId={lastId}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var cachedResult = JsonSerializer.Deserialize<PaginatedResult<Products>>(cached);
                return Ok(cachedResult);
            }
            if (User.Identity.IsAuthenticated)
            {
                var user = User.Identity.Name;
            }
            var result = await _productrepo.GetProductsAsync(new PaginationParams
            {
                PageSize = pageSize,
                LastId=lastId
            });
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Products>> GetProductById(int id)
        {
            var cacheKey = $"Products:Id={id}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var cachedProduct = JsonSerializer.Deserialize<Products>(cached);
                return Ok(cachedProduct);
            }
            var product = await _productrepo.GetProductById(id);

            if (product == null)
            {
                return NotFound();
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            return Ok(product);
        }
        
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<Products>>> GetProductsByCategory(int categoryId)
        {
            var cachedKey=$"Products:Category={categoryId}";
            var cached = await _cache.GetStringAsync(cachedKey);
            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Products>>(cached));
            }

            var products = await _productrepo.GetProductsByCategory(categoryId);

                await _cache.SetStringAsync(cachedKey, JsonSerializer.Serialize(products), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
            
            return Ok(products);
        }
        

    }
}

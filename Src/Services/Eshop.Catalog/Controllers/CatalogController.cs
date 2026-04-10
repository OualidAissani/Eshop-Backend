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
        //Need Change
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto product, List<IFormFile> formFile,
            [FromHeader(Name = "x-Idempotency-Key")] string key,CancellationToken ct)
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
            var cacheKey = $"Idempotency:Product:Create:{key}";

            var cached = await _cache.GetAsync(cacheKey);

            if (cached != null)
            {
                return CreatedAtAction(nameof(GetProductById), new { id = JsonSerializer.Deserialize<Products>(cached)?.Id },JsonSerializer.Deserialize<Products>(cached) ?? null);
            }

            var result = await _productrepo.CreateProduct(product,formFile,ct); 

            if(result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }
         
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result.Value), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
            if (result.Value?.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _cache.RemoveAsync($"Products:Category={category.Id}");
                }
            }
            return CreatedAtAction(nameof(GetProductById),new {id=result.Value?.Id},result.Value);
        }
        
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id,[FromForm] ProductsUpdateDto product, List<IFormFile> formFile ,
            [FromHeader(Name = "x-Idempotency-Key")] string key,CancellationToken ct)
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
           

            var result = await _productrepo.UpdateProduct(id,product, formFile, ct);


            if(result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            await _cache.RemoveAsync($"Products:Id={result.Value.Id}");

            if (result.Value.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _cache.RemoveAsync($"Products:Category={category.Id}");
                }
            }

            return Ok(result.Value);

        }

        [HttpDelete("{Id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int Id,CancellationToken ct)
        {
            var product = await _productrepo.GetProductById(Id, ct);
            var result = await _productrepo.DeleteProduct(Id,ct);
            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }
            await _cache.RemoveAsync($"Products:Id={Id}");
            await _cache.RemoveAsync($"Products:List:*");
            if (product?.Categories != null)
            {
                foreach (var category in product.Categories)
                {
                    await _cache.RemoveAsync($"Products:Category={category.Id}");
                }
            }
            return Ok(new {message="The Product Has Been Deleted Successfully"});
        }
        
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<Products>>> GetProducts([FromQuery] int? lastId, CancellationToken ct, [FromQuery] int pageSize = 10)
        {
            var cacheKey= $"Products:List:PageSize={pageSize}:LastId={lastId}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var cachedResult = JsonSerializer.Deserialize<PaginatedResult<ProductDto>>(cached);
                return Ok(cachedResult);
            }

            var result = await _productrepo.GetProductsAsync(new PaginationParams
            {
                PageSize = pageSize,
                LastId=lastId
            },ct);
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Products>> GetProductById(int id, CancellationToken ct)
        {
            var cacheKey = $"Products:Id={id}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var cachedProduct = JsonSerializer.Deserialize<Products>(cached);
                return Ok(cachedProduct);
            }
            var product = await _productrepo.GetProductById(id, ct);

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
        public async Task<ActionResult<List<Products>>> GetProductsByCategory(int categoryId, CancellationToken ct)
        {
            var cachedKey=$"Products:Category={categoryId}";
            var cached = await _cache.GetStringAsync(cachedKey);
            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Products>>(cached));
            }

            var products = await _productrepo.GetProductsByCategory(categoryId,ct);

                await _cache.SetStringAsync(cachedKey, JsonSerializer.Serialize(products), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
            
            return Ok(products);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q,CancellationToken ct)
        {
            if (q == null)
            {
                return BadRequest("You To Add A Search Term/Tag");
            }
            var chachedKey=$"Products:Search={q}";
            var cached = await _cache.GetStringAsync(chachedKey);
            if (cached != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Products>>(cached));
            }

            var products=await _productrepo.ProductSearch(q,ct);
            if (products == null)
            {
                return NotFound();
            }
                await _cache.SetStringAsync(chachedKey, JsonSerializer.Serialize(products), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });

            return Ok(products);
        }
        

    }
}

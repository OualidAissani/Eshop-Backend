using Eshop.Catalog.Dtos;
using Eshop.Catalog.Entities;
using Eshop.Catalog.Services.IServices;
using FluentResults;
using REDox.Json;
using StackExchange.Redis;


namespace Eshop.Catalog.Services
{
    public class CachedProductService : IProductService
    {
        private readonly IProductService _productService;
        private readonly IDatabase _redisdb;
        public CachedProductService(IDatabase redisDb, IProductService productService)
        {
            _redisdb = redisDb;
            _productService = productService;
        }

        public async Task<Result<ProductDto>> ApplyProductDiscount(ProductDto product, CancellationToken ct)
        {
            return await _productService.ApplyProductDiscount(product, ct);
        }

        public async Task<Result<bool>> AssignProductToCategory(int productId, int categoryId, CancellationToken ct)
        {
            return await _productService.AssignProductToCategory(productId,categoryId,ct);
        }

        public async Task<Result<ProductDto>> CreateProduct(ProductCreateDto product, List<IFormFile>? formFile, CancellationToken ct)
        {
            if (product == null)
            {
                return Result.Fail("Product Data Is Required");
            }
            if (formFile == null || formFile.Count == 0)
            {
                return Result.Fail("Atleast One Image Attached To The Product");
            }
            if (product.IdempotencyKey== null)
            {
                return Result.Fail("Idempotency Key is required");
            }
            var cacheKey = $"Idempotency:Product:Create:{product.IdempotencyKey}";

            var reserved= await _redisdb.StringSetAsync(cacheKey,"in-progress",TimeSpan.FromHours(24),When.NotExists);

            if (!reserved)
            {
                var cached = await _redisdb.StringGetAsync(cacheKey);
                if (cached== "in-progress")
                {
                    return Result.Fail("");
                }
                var cachedProduct = JsonSerializer.Deserialize<ProductDto>(cached.ToString());
                return cachedProduct;
            }

            var result = await _productService.CreateProduct(product, formFile, ct);

            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }

            await _redisdb.StringSetAsync(cacheKey, JsonSerializer.Serialize(result.Value), TimeSpan.FromHours(24));
            if (result.Value?.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _redisdb.KeyDeleteAsync($"Products:Category={category.Id}");
                }
            }
            return result.Value;
        }

        public async Task<Result<bool>> DeleteProduct(int productId, CancellationToken ct)
        {
            var result = await _productService.DeleteProductReturnOldProduct(productId, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }
            await _redisdb.KeyDeleteAsync($"Products:Id={productId}");
            if (result.Value?.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _redisdb.KeyDeleteAsync($"Products:Category={category.Id}");
                }
            }
            return true;
        }

        public async Task<Result<ProductDto>> DeleteProductReturnOldProduct(int productId, CancellationToken ct)
        {
            var result = await _productService.DeleteProductReturnOldProduct(productId, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }
            await _redisdb.KeyDeleteAsync($"Products:Id={productId}");
            if (result.Value?.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _redisdb.KeyDeleteAsync($"Products:Category={category.Id}");
                }
            }
            return result;
        }


        public async Task<List<ProductDto>> GetHeroProducts(CancellationToken ct)
        {
            var cacheKey = "Products:Hero";

                var cached = await _redisdb.StringGetAsync(cacheKey);
                if (cached.HasValue)
                return JsonSerializer.Deserialize<List<ProductDto>>(cached.ToString());


            var products = await _productService.GetHeroProducts(ct);

            await _redisdb.StringSetAsync(cacheKey, JsonSerializer.Serialize(products), TimeSpan.FromHours(24));

            return products;
        }

        public async Task<ProductDto> GetProductById(int productId, CancellationToken ct)
        {
            var cacheKey = $"Products:Id={productId}";

                var cached = await _redisdb.StringGetAsync(cacheKey);
                if (cached.HasValue)
                {
                return JsonSerializer.Deserialize<ProductDto>(cached.ToString());
            }
            
            var product = await _productService.GetProductById(productId, ct);

            if (product == null)
            {
                return null;
            }
            await _redisdb.StringSetAsync(cacheKey, JsonSerializer.Serialize(product),TimeSpan.FromHours(24));
            return product;
        }

        public async Task<List<ProductPriceDto>> GetProductPrice(List<int> ProductId, CancellationToken ct)
        {
            return await _productService.GetProductPrice(ProductId, ct);
        }

        public async Task<PaginatedResult<ProductDto>> GetProductsAsync(PaginationParams paging, CancellationToken ct)
        {
            var result = await _productService.GetProductsAsync(new PaginationParams
            {
                PageSize = paging.PageSize,
                LastId = paging.LastId
            }, ct);
          
            return result;
        }

        public async Task<List<ProductDto>> GetProductsByCategory(int categoryId, CancellationToken ct)
        {
            var cachedKey = $"Products:Category={categoryId}";

                var cached = await _redisdb.StringGetAsync(cachedKey);
                if (cached.HasValue)
                return JsonSerializer.Deserialize<List<ProductDto>>(cached.ToString());
           

            var products = await _productService.GetProductsByCategory(categoryId, ct);

            await _redisdb.StringSetAsync(cachedKey, JsonSerializer.Serialize(products),TimeSpan.FromHours(24));

            return products;
        }

        public async Task<List<ProductDto>> ProductSearch(string tag, CancellationToken ct)
        {
            if (tag == null)
            {
                return null;
            }
            var cachedKey = $"Products:Search={tag}";
            var reserved = await _redisdb.StringSetAsync(cachedKey, "in-progress", TimeSpan.FromHours(24),When.NotExists);
            if (!reserved)
            {
                var cached = await _redisdb.StringGetAsync(cachedKey);
                    if (cached == "in-progress")
                    return null;

                return JsonSerializer.Deserialize<List<ProductDto>>(cached.ToString());
            }

            var products = await _productService.ProductSearch(tag, ct);
            if (products == null)
            {
                return null;
            }
            await _redisdb.StringSetAsync(cachedKey, JsonSerializer.Serialize(products),TimeSpan.FromHours(24));

            return products;
        }

        public async Task<Result<ProductDto>> UpdateHeroSelection(int productId, ProductHeroUpdateDto dto, CancellationToken ct)
        {
            var result = await _productService.UpdateHeroSelection(productId, dto, ct);

            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }

            await _redisdb.KeyDeleteAsync($"Products:Id={productId}");
            await _redisdb.KeyDeleteAsync("Products:Hero");

            return result.Value;
        }

        public async Task<Result<ProductDto>> UpdateProduct(int ProductId, ProductsUpdateDto productDto, List<IFormFile>? formFile, CancellationToken ct, bool ImageAppend = false)
        {

            var result = await _productService.UpdateProduct(ProductId, productDto, formFile, ct, ImageAppend);


            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }


            await _redisdb.KeyDeleteAsync($"Products:Id={result.Value.Id}");

            if (result.Value.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _redisdb.KeyDeleteAsync($"Products:Category={category.Id}");
                }
            }

            return result.Value;
        }
    }
}

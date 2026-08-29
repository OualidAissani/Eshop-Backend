using Eshop.Catalog.Dtos;
using Eshop.Catalog.Entities;
using Eshop.Catalog.Services.IServices;
using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Eshop.Catalog.Services
{
    public class CachedProductService : IProductService
    {
        private readonly IProductService _productService;
        private readonly IDistributedCache _cache;
        public CachedProductService(IDistributedCache cache, IProductService productService)
        {
            _cache = cache;
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

            var cached = await _cache.GetAsync(cacheKey);

            if (cached != null)
            {
                var cachedProduct = JsonSerializer.Deserialize<ProductDto>(cached);
                return cachedProduct;
            }

            var result = await _productService.CreateProduct(product, formFile, ct);

            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
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
            return result.Value;
        }

        public async Task<Result<bool>> DeleteProduct(int productId, CancellationToken ct)
        {
            var result = await _productService.DeleteProductReturnOldProduct(productId, ct);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }
            await _cache.RemoveAsync($"Products:Id={productId}");
            if (result.Value?.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _cache.RemoveAsync($"Products:Category={category.Id}");
                }
            }
            return true;
        }

        public async Task<Result<ProductDto>> DeleteProductReturnOldProduct(int productId, CancellationToken ct)
        {
            var result = await _productService.DeleteProductReturnOldProduct(productId, ct);
            if (result.IsFailed)
            {
                return null;
            }
            await _cache.RemoveAsync($"Products:Id={productId}");
            if (result.Value?.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _cache.RemoveAsync($"Products:Category={category.Id}");
                }
            }
            return result;
        }


        public async Task<List<ProductDto>> GetHeroProducts(CancellationToken ct)
        {
            var cacheKey = "Products:Hero";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<List<ProductDto>>(cached);
            }

            var products = await _productService.GetHeroProducts(ct);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(products), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return products;
        }

        public async Task<ProductDto> GetProductById(int productId, CancellationToken ct)
        {
            var cacheKey = $"Products:Id={productId}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var cachedProduct = JsonSerializer.Deserialize<ProductDto>(cached);
                return cachedProduct;
            }
            var product = await _productService.GetProductById(productId, ct);

            if (product == null)
            {
                return null;
            }
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
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
            var cached = await _cache.GetStringAsync(cachedKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<List<ProductDto>>(cached);
            }

            var products = await _productService.GetProductsByCategory(categoryId, ct);

            await _cache.SetStringAsync(cachedKey, JsonSerializer.Serialize(products), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            return products;
        }

        public async Task<List<ProductDto>> ProductSearch(string tag, CancellationToken ct)
        {
            if (tag == null)
            {
                return null;
            }
            var chachedKey = $"Products:Search={tag}";
            var cached = await _cache.GetStringAsync(chachedKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<List<ProductDto>>(cached);
            }

            var products = await _productService.ProductSearch(tag, ct);
            if (products == null)
            {
                return null;
            }
            await _cache.SetStringAsync(chachedKey, JsonSerializer.Serialize(products), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            return products;
        }

        public async Task<Result<ProductDto>> UpdateHeroSelection(int productId, ProductHeroUpdateDto dto, CancellationToken ct)
        {
            var result = await _productService.UpdateHeroSelection(productId, dto, ct);

            if (result.IsFailed)
            {
                return null;
            }

            await _cache.RemoveAsync($"Products:Id={productId}");
            await _cache.RemoveAsync("Products:Hero");

            return result.Value;
        }

        public async Task<Result<ProductDto>> UpdateProduct(int ProductId, ProductsUpdateDto productDto, List<IFormFile>? formFile, CancellationToken ct, bool ImageAppend = false)
        {

            var result = await _productService.UpdateProduct(ProductId, productDto, formFile, ct, ImageAppend);


            if (result.IsFailed)
            {
                return Result.Fail(result.Errors.First().Message);
            }


            await _cache.RemoveAsync($"Products:Id={result.Value.Id}");

            if (result.Value.Categories != null)
            {
                foreach (var category in result.Value.Categories)
                {
                    await _cache.RemoveAsync($"Products:Category={category.Id}");
                }
            }

            return result.Value;
        }
    }
}

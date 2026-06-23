using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Eshop.Events;
using FluentResults;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using Polly;

namespace Eshop.Catalog.Services;

    public class ProductService: IProductService
    {
        private readonly MongoCatalogContext _mongoContext;
        private readonly IMediaService _mediaService;
        private readonly ILogger<ProductService> _logger;
        private readonly IPublishEndpoint _publish;

        public ProductService(
            MongoCatalogContext mongoContext,
            IMediaService mediaService,
            ILogger<ProductService> logger,
            IConfiguration configurations,
            IHttpClientFactory httpClientFactory,
            IPublishEndpoint publish
            )
        {
            _mongoContext = mongoContext;
            _mediaService = mediaService;
            _logger = logger;
            _publish = publish;
        }
        public async Task<List<ProductPriceDto>> GetProductPrice(List<int> ProductId, CancellationToken ct)
        {
            var productFilter = Builders<ProductDocument>.Filter.In(p => p.ProductId, ProductId);
            var products = await _mongoContext.Products.Find(productFilter).ToListAsync(ct);
            return products.Select(i => new ProductPriceDto
            {
                Id = i.ProductId,
                Price = i.Price,
                Name = i.Title
            }).ToList();
        }

        public async Task<Result<ProductDto>> CreateProduct(ProductCreateDto product, List<IFormFile> formFile, CancellationToken ct)
        {

            ArgumentNullException.ThrowIfNull(product);
            if(formFile == null || formFile.Count == 0)
            {
                return Result.Fail<ProductDto>("Atleast One Image Attached To The Product");
            }

            var productId = await GetNextProductId(ct);
            var categories = await GetCategoriesByIds(product.Categories, ct);

            var productDocument = new ProductDocument
            {
                ProductId = productId,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                Status = product.Status,
                SpecialStatus = product.SpecialStatus,
                DisplayOrder = product.DisplayOrder,
                Categories = categories,
                Attributes = product.Attributes ?? new Dictionary<string, string>()
            };

            var mediaItems = new List<ProductMediaItem>();
            foreach (var file in formFile)
            {
                using var stream = file.OpenReadStream();
                var media = new ProductMediaItem
                {
                    Description = product.Description
                };

                var createdMedia = await _mediaService.CreateMedia(media, stream, file.ContentType ?? "application/octet-stream", file.FileName, ct);
                mediaItems.Add(new ProductMediaItem
                {
                    Media = createdMedia.Media,
                    Description = createdMedia.Description
                });
            }

            productDocument.Media = mediaItems;

            await _mongoContext.Products.InsertOneAsync(productDocument, cancellationToken: ct);

            return ToProductDto(productDocument);
        }

        public async Task<Result<ProductDto>> UpdateProduct(int ProductId, ProductsUpdateDto productDto, List<IFormFile>? formFile, CancellationToken ct, bool ImageAppend = false)
        {

            var product = await _mongoContext.Products
                .Find(i => i.ProductId == ProductId)
                .FirstOrDefaultAsync(ct);

            if (product == null)
            {
                return Result.Fail<ProductDto>($"The product with Id {ProductId} Not Found");
            }

            if (!ImageAppend && formFile != null && formFile.Count > 0)
            {
                await DeleteOldProductMedia(product, ct);
            }

            await UpdateProductDocument(ProductId, productDto, formFile, product, ImageAppend, ct);

            var updateResult = await _mongoContext.Products.ReplaceOneAsync(
                p => p.ProductId == ProductId,
                product,
                new ReplaceOptions { IsUpsert = false },
                ct);

            if (updateResult.ModifiedCount == 0 && updateResult.MatchedCount == 0)
            {
                return Result.Fail<ProductDto>("Failed To Update Product");
            }

            await _publish.Publish(new UpdateCartProduct(product.ProductId, product.Title, product.Price));

            return ToProductDto(product);
        }


        public async Task<Result<bool>> DeleteProduct(int productId, CancellationToken ct)
        {
            if(productId<= 0)
            {
                throw new ArgumentException("Product Id is not valid");
            }
            var product = await _mongoContext.Products
                .Find(i => i.ProductId == productId)
                .FirstOrDefaultAsync(ct);

            if (product == null)
            {
                return Result.Fail<bool>($"The product with Id {productId} Not Found");
            }
            var media = await Task.WhenAll(product.Media.Select(s => _mediaService.DeleteMedia(s.Media, ct)));
            if (media.Any(r => !r))
            {
                return Result.Fail<bool>("There Was An Issue Deleting The Product");
            }

            var deleteResult = await _mongoContext.Products.DeleteOneAsync(i => i.ProductId == productId, ct);
            if (deleteResult.DeletedCount == 0)
            {
                return Result.Fail<bool>("There Was An Issue Deleting The Product");
            }

            await _publish.Publish(new DeleteInventory(productId));

            await _publish.Publish(new DeleteCartProduct(productId));

            return true;
        }

        public async Task<Result<ProductDto>> DeleteProductReturnOldProduct(int productId, CancellationToken ct)
        {
            if (productId <= 0)
            {
                throw new ArgumentException("Product Id is not valid");
            }
            var product = await _mongoContext.Products
                .Find(i => i.ProductId == productId)
                .FirstOrDefaultAsync(ct);
            if (product == null)
            {
                return Result.Fail<ProductDto>($"The product with Id {productId} Not Found");
            }
            var media = await Task.WhenAll(product.Media.Select(s => _mediaService.DeleteMedia(s.Media, ct)));
            if (media.Any(r => !r))
            {
                return Result.Fail<ProductDto>("There Was An Issue Deleting The Product");
            }

            var deleteResult = await _mongoContext.Products.DeleteOneAsync(i => i.ProductId == productId, ct);
            if (deleteResult.DeletedCount == 0)
            {
                return Result.Fail<ProductDto>("There Was An Issue Deleting The Product");
            }
        await _publish.Publish(new DeleteInventory(productId));


        await _publish.Publish(new DeleteCartProduct(productId));

            return ToProductDto(product);
        }

        public async Task<ProductDto> GetProductById(int productId, CancellationToken ct)
        {
            var product = await _mongoContext.Products
                .Find(p => p.ProductId == productId)
                .FirstOrDefaultAsync(ct);

            return product == null ? null : ToProductDto(product);
        }

        public async Task<List<ProductDto>> GetAllProducts(CancellationToken ct)
        {
            var products = await _mongoContext.Products
                .Find(FilterDefinition<ProductDocument>.Empty)
                .SortBy(d => d.DisplayOrder)
                .ThenBy(d => d.ProductId)
                .ToListAsync(ct);
            return products.Select(ToProductDto).ToList();
        }

        public async Task<List<ProductDto>> GetProductsByCategory(int categoryId, CancellationToken ct)
        {
            var filter = Builders<ProductDocument>.Filter.ElemMatch(p => p.Categories, c => c.Id == categoryId);
        var products = await _mongoContext.Products
            .Find(filter)
            .SortBy(d => d.DisplayOrder)
            .ThenBy(d => d.ProductId)
            .ToListAsync(ct);
        return products.Select(ToProductDto).ToList();
        }

        public async Task<List<ProductDto>> ProductSearch(string tag,CancellationToken ct)
        {
           var regex = new BsonRegularExpression(tag, "i");
           var filterBuilder = Builders<ProductDocument>.Filter;
           var filter = filterBuilder.Or(
               filterBuilder.Regex(p => p.Title, regex),
               filterBuilder.Regex(p => p.Description, regex),
               filterBuilder.ElemMatch(
                   p => p.Categories,
                   c => c.Title != null && c.Title.ToLower().Contains(tag.ToLower())),
               filterBuilder.ElemMatch(
                   p => p.Categories,
                   c => c.Description != null && c.Description.ToLower().Contains(tag.ToLower())));

           var results = await _mongoContext.Products.Find(filter).ToListAsync(ct);
           return results.Select(ToProductDto).ToList();
        }

        public async Task<PaginatedResult<ProductDto>> GetProductsAsync(PaginationParams paging, CancellationToken ct)
        {
            paging.Validate();

            var filter = paging.LastId.HasValue
                ? Builders<ProductDocument>.Filter.Gt(p => p.ProductId, paging.LastId.Value)
                : FilterDefinition<ProductDocument>.Empty;

            var total = await _mongoContext.Products.CountDocumentsAsync(FilterDefinition<ProductDocument>.Empty, cancellationToken: ct);
            var items = await _mongoContext.Products
                .Find(filter)
                .SortBy(d => d.DisplayOrder)
            .ThenBy(d => d.ProductId)
                .Limit(paging.PageSize + 1)
                .ToListAsync(ct);

            int? nextCursor = null;
            if (items.Count > paging.PageSize)
            {
                items.RemoveAt(items.Count - 1);
                nextCursor = items[^1].ProductId;
            }

            return new PaginatedResult<ProductDto>
            {
                Items = items.Select(ToProductDto).ToList(),
                PageSize = paging.PageSize,
                NextCursor= nextCursor,
                Total = (int)total
            };
        }

        public async Task<Result<bool>> AssignProductToCategory(int productId, int categoryId,CancellationToken ct)
        {
            if(productId<=0 || categoryId<=0)
            {
               throw new ArgumentException("Product or Category isnt valid");
            }

            var product = await _mongoContext.Products
                .Find(p => p.ProductId == productId)
                .FirstOrDefaultAsync(ct);

            if (product == null)
            {
                return Result.Fail($"Product with id {productId} not found");
            }

            if (product.Categories.Any(i => i.Id == categoryId))
            {
                return true;
            }

            var category = await _mongoContext.Categories.Find(i => i.CategoryId == categoryId).FirstOrDefaultAsync(ct);
            if (category == null)
            {
                return Result.Fail($"Category with id {categoryId} not found");
            }

            product.Categories.Add(new CategoryItem
            {
                Id = category.CategoryId,
                Title = category.Title,
                Description = category.Description
            });

            var updateResult = await _mongoContext.Products.ReplaceOneAsync(
                p => p.ProductId == productId,
                product,
                new ReplaceOptions { IsUpsert = false },
                ct);

            if (updateResult.ModifiedCount == 0 && updateResult.MatchedCount == 0)
            {
                return Result.Fail("Failed to assign product to category");
            }
            return true;

        }




        private async Task UpdateProductDocument(int ProductId, ProductsUpdateDto productDto, List<IFormFile>? formFile, ProductDocument product, bool imageAppend, CancellationToken ct)
        {
            if (productDto.CategoriesId != null && productDto.CategoriesId.Count > 0)
            {
                var categories = await _mongoContext.Categories
                    .Find(c => productDto.CategoriesId.Contains(c.CategoryId))
                    .ToListAsync(ct);

                product.Categories = categories.Select(c => new CategoryItem
                {
                    Id = c.CategoryId,
                    Title = c.Title,
                    Description = c.Description
                }).ToList();
            }

            if (formFile != null && formFile.Count > 0)
            {
                if (!imageAppend)
                {
                    product.Media.Clear();
                }
                foreach (var file in formFile)
                {
                    var media = new ProductMediaItem
                    {
                        Description = productDto.Description ?? product.Description
                    };
                    using var stream = file.OpenReadStream();

                    var createdMedia = await _mediaService.CreateMedia(media, stream, file.ContentType ?? "application/octet-stream", file.FileName, ct);
                    product.Media.Add(new ProductMediaItem
                    {
                        Media = createdMedia.Media,
                        Description = createdMedia.Description
                    });
                }
            }

            product.Title = productDto.Title ?? product.Title;
            product.Description = productDto.Description ?? product.Description;
            product.Price = productDto.Price <= 0 ? product.Price : productDto.Price;
            product.Status = productDto.Status;
            product.SpecialStatus = productDto.SpecialStatus;
            product.DisplayOrder = productDto.DisplayOrder ?? product.DisplayOrder;
        if (product.HeroImageUrl != null && !product.Media.Any(m => m.Media == product.HeroImageUrl))
        {
            product.HeroImageUrl = null; // falls back to media[0] on the frontend
        }
        if (productDto.Attributes != null)
            {
                product.Attributes = productDto.Attributes;
            }
        }

        private async Task DeleteOldProductMedia(ProductDocument product, CancellationToken ct)
        {
            var mediaRetryPolicy = Policy
                .Handle<InvalidOperationException>()
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Media deletion failed. Retry {retryCount}/3 after {timespan.TotalSeconds}s. Error: {exception.Message}");
                    });

            await mediaRetryPolicy.ExecuteAsync(async () =>
            {
                var deletionResult = await Task.WhenAll(product.Media.Select(m => _mediaService.DeleteMedia(m.Media, ct)));

                if (!deletionResult.All(r => r))
                {
                    throw new InvalidOperationException("The Media Deletion Process Failed");
                }
            });
        }

        private async Task<List<CategoryItem>> GetCategoriesByIds(List<int>? categoryIds, CancellationToken ct)
        {
            if (categoryIds == null || categoryIds.Count == 0)
            {
                return new List<CategoryItem>();
            }

            var categories = await _mongoContext.Categories
                .Find(c => categoryIds.Contains(c.CategoryId))
                .ToListAsync(ct);

            return categories.Select(c => new CategoryItem
            {
                Id = c.CategoryId,
                Title = c.Title,
                Description = c.Description
            }).ToList();
        }

        private ProductDto ToProductDto(ProductDocument product)
        {
            return new ProductDto
            {
                Id = product.ProductId,
                Title = product.Title,
                Status = product.Status,
                SpecialStatus = product.SpecialStatus,
                Description = product.Description,
                DisplayOrder = product.DisplayOrder,
                Price = product.Price,
                Media = product.Media.Select(m => new MediaDto { MediaUrl = m.Media }).ToList(),
                IsHeroFeatured = product.IsHeroFeatured,
                HeroOrder = product.HeroOrder,
                HeroImageUrl = product.HeroImageUrl,
                Categories = product.Categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Description = c.Description,
                    Name = c.Title
                }).ToList(),
                Attributes = product.Attributes ?? new Dictionary<string, string>()
            };
        }

        private async Task<int> GetNextProductId(CancellationToken ct)
        {
            var update = Builders<CounterDocument>.Update.Inc(c => c.Value, 1);
            var options = new FindOneAndUpdateOptions<CounterDocument, CounterDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            // No-op patch placeholder before explicit type annotation for FindOneAndUpdateAsync.
            CounterDocument counter = await _mongoContext.Counters
                .FindOneAndUpdateAsync<CounterDocument, CounterDocument>(
                    c => c.Name == "products",
                    update,
                    options,
                    ct);

            return counter.Value;
        }
    public async Task<Result<ProductDto>> UpdateHeroSelection(int productId, ProductHeroUpdateDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var product = await _mongoContext.Products
            .Find(p => p.ProductId == productId)
            .FirstOrDefaultAsync(ct);

        if (product == null)
        {
            return Result.Fail<ProductDto>($"The product with Id {productId} Not Found");
        }

        if (dto.IsHeroFeatured)
        {
            if (dto.HeroOrder is < 1 or > 3)
            {
                return Result.Fail<ProductDto>("Hero position must be 1, 2, or 3.");
            }

            if (dto.HeroImageUrl != null && !product.Media.Any(m => m.Media == dto.HeroImageUrl))
            {
                return Result.Fail<ProductDto>("Selected cover image is not one of this product's uploaded images.");
            }

            product.IsHeroFeatured = true;
            product.HeroOrder = dto.HeroOrder;
            product.HeroImageUrl = dto.HeroImageUrl;
        }
        else
        {
            product.IsHeroFeatured = false;
            product.HeroOrder = null;
        }

        var updateResult = await _mongoContext.Products.ReplaceOneAsync(
            p => p.ProductId == productId,
            product,
            new ReplaceOptions { IsUpsert = false },
            ct);

        if (updateResult.ModifiedCount == 0 && updateResult.MatchedCount == 0)
        {
            return Result.Fail<ProductDto>("Failed to update hero selection");
        }

        return ToProductDto(product);
    }

    public async Task<List<ProductDto>> GetHeroProducts(CancellationToken ct)
    {
        var products = await _mongoContext.Products
            .Find(p => p.IsHeroFeatured)
            .SortBy(p => p.HeroOrder)
            .Limit(3)
            .ToListAsync(ct);

        return products.Select(ToProductDto).ToList();
    }

}


using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Eshop.Events;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Catalog.Services
{
    public class ProductService: IProductService
    {
        private readonly CatalogDbContext _context;
        private readonly IMediaService _mediaService;
        private readonly ILogger<ProductService> _logger;
        private readonly IPublishEndpoint _publish;

        public ProductService(
            CatalogDbContext context,
            IMediaService mediaService,
            ILogger<ProductService> logger,
            IConfiguration configurations,
            IHttpClientFactory httpClientFactory,
            IPublishEndpoint publish)
        {
            _context = context;
            _mediaService = mediaService;
            _logger = logger;
            _publish = publish;
        }
        public async Task<List<ProductPriceDto>> GetProductPrice(List<int> ProductId)
        {
            return await _context
                .Products
                .Where(i => ProductId.Contains(i.Id))
                .AsNoTracking()
                .Select(i => new ProductPriceDto{
                    Id = i.Id,
                    Price = i.Price
                })
                .ToListAsync();
        }

        public async Task<dynamic> CreateProduct(ProductCreateDto product,CancellationToken ct)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var productobj = new Products()
                {
                    Title= product.Title,
                    Description= product.Description,
                    Price= product.Price,
                    Status= product.Status,
                    SpecialStatus= product.SpecialStatus,
                    DisplayOrder= product.DisplayOrder,
                };
                if(product.Categories!=null && product.Categories.Count>0)
                {
                    var categories = await _context.Categories.Where(c => product.Categories.Contains(c.Id)).ToListAsync();
                    productobj.Categories = categories;
                }
                _context.Products.Add(productobj);
                var result = await _context.SaveChangesAsync(ct);

                return new { productobj.Id, productobj.Description };
            });
        }
        
        public async Task<ProductDto> UpdateProduct(Products product,Stream mediafile,string contentType,string filename,CancellationToken ct)
        {
            if (mediafile != null)
            {
                var media = new ProductMedia()
                {
                    ProductId = product.Id,
                    Description = product.Description,
                };
                await _mediaService.CreateMedia(media,mediafile, contentType, filename,ct);
            }
            _context.Products.Update(product);

            var result=await _context.SaveChangesAsync(ct);

            if(result<=0)
            {
                throw new Exception("Failed to update product");
            }
            
            await _publish.Publish(new UpdateCartProduct(product.Id, product.Title, product.Price));

            return new ProductDto()
            {
                Id = product.Id,
                Description = product.Description,
                Title = product.Title,
                Price = product.Price,
                Categories = product.Categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Title, Description = c.Description }).ToList(),
                Media = product.Media.Select(c => new MediaDto { MediaUrl = c.Media }).ToList()
            };
        }
        
        public async Task<bool> DeleteProduct(int productId, CancellationToken ct)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var product = await _context.Products.Include(i => i.Media).Where(I => I.Id == productId).FirstOrDefaultAsync();
                if (product == null)
                {
                    return false;
                }
                var media = await Task.WhenAll(product.Media.Select(s => _mediaService.DeleteMedia(s.Media,ct)));
                _context.Products.Remove(product);

                var result = await _context.SaveChangesAsync(ct);
                return true;
            });
        }
        
        public async Task<ProductDto> GetProductById(int productId)
        {
            return await _context.Products
                .Select(i => new ProductDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    Status = i.Status,
                    SpecialStatus = i.SpecialStatus,
                    Description = i.Description,
                    DisplayOrder = i.DisplayOrder,
                    Price = i.Price,
                    Media = i.Media.Select(m => new MediaDto { MediaUrl = m.Media }).ToList(),
                    Categories = i.Categories.Select(c => new CategoryDto { Id = c.Id, Description = c.Description, Name = c.Title }).ToList()

                })
                .FirstOrDefaultAsync(p => p.Id == productId);
        }
        
        public async Task<List<ProductDto>> GetAllProducts()
        {
            return await _context.Products
                .Select(i => new ProductDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    Status = i.Status,
                    SpecialStatus = i.SpecialStatus,
                    Description = i.Description,
                    DisplayOrder = i.DisplayOrder,
                    Price = i.Price,
                    Media = i.Media.Select(m => new MediaDto { MediaUrl = m.Media }).ToList(),
                    Categories = i.Categories.Select(c => new CategoryDto { Id = c.Id, Description = c.Description, Name = c.Title }).ToList()

                })
                .OrderBy(d => d.DisplayOrder == null ? d.Id : d.DisplayOrder)
                .ToListAsync();            
        }
        
        public async Task<List<ProductDto>> GetProductsByCategory(int categoryId)
        {
            return await _context.Products
                .Where(c => c.Categories.Any(cat => cat.Id == categoryId))
                .Select(i => new ProductDto
            {
                Id = i.Id,
                Title = i.Title,
                Status = i.Status,
                SpecialStatus = i.SpecialStatus,
                Description = i.Description,
                DisplayOrder = i.DisplayOrder,
                Price = i.Price,
                Media = i.Media.Select(m => new MediaDto { MediaUrl = m.Media }).ToList(),
                Categories = i.Categories.Select(c => new CategoryDto { Id = c.Id, Description = c.Description, Name = c.Title }).ToList()

            })                
                .OrderBy(d => d.DisplayOrder == null ? d.Id : d.DisplayOrder)
                .ToListAsync();
        }
        
        public async Task<List<Products>> ProductSearch(string tag)
        {
           return await _context.Products
                .Include(i=>i.Categories)
                .AsNoTracking()
                .AsSplitQuery()
                .Where(p=>
                    (EF.Functions.TrigramsAreSimilar(p.Description,tag))
                ||  (EF.Functions.TrigramsAreSimilar(p.Title,tag))
                ||  (p.Categories.Any(i=>EF.Functions.TrigramsAreSimilar(i.Title,tag)))
                ||  (p.Categories.Any(i => EF.Functions.TrigramsAreSimilar(i.Description, tag))))
                .ToListAsync();
        }

        public async Task<PaginatedResult<ProductDto>> GetProductsAsync(PaginationParams paging)
        {
            paging.Validate();

            var query = _context.Products.AsQueryable();

            int total = await query.CountAsync();

            if (paging.LastId.HasValue)
            {
                query = query.Where(p => p.Id > paging.LastId.Value);
            }

            var items = await query.Select(i => new ProductDto
            {
               Id=i.Id,
               Title= i.Title,
               Status= i.Status,
               SpecialStatus= i.SpecialStatus,
               Description= i.Description,
               DisplayOrder= i.DisplayOrder,
               Price= i.Price,
               Media= i.Media.Select(m => new MediaDto {MediaUrl=m.Media}).ToList(),
               Categories=i.Categories.Select(c=> new CategoryDto {Id=c.Id,Description=c.Description,Name=c.Title }).ToList()

            })
            .OrderBy(p=>p.Id)
            .Take(paging.PageSize+1)
            .ToListAsync();

            int? nextCursor = null;
            if (items.Count > paging.PageSize)
            {
                items.RemoveAt(items.Count - 1);
                nextCursor = items[^1].Id;
            }

            return new PaginatedResult<ProductDto>
            {
                Items = items,
                PageSize = paging.PageSize,
                NextCursor= nextCursor,
                Total = total
            };
        }

        public async Task<bool> AssignProductToCategory(int productId, int categoryId,CancellationToken ct)
        {
            var product=await _context.Products.Include(p => p.Categories).FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                _logger.LogError("Product with id {id} not found", productId);
                return false;
            }

            if (product.Categories.Any(i => i.Id == categoryId))
            {
                return true;
            }

            var category = await _context.Categories.FindAsync(new object[] { categoryId }, ct);
            if (category == null)
            {
                _logger.LogError("Category with id {id} not found", categoryId);
                return false;
            }

            product.Categories.Add(category);

            if (await _context.SaveChangesAsync(ct) == 0)
            {
                return false;
            }
            return true;

        }
    }
}

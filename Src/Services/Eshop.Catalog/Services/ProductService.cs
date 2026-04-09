using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Eshop.Events;
using FluentResults;
using MassTransit;
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
            IPublishEndpoint publish
            )
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
                    Price = i.Price,
                    Name=i.Title
                })
                .ToListAsync();
        }

        public async Task<Result<ProductDto>> CreateProduct(ProductCreateDto product, List<IFormFile> formFile, CancellationToken ct)
        {

            ArgumentNullException.ThrowIfNull(product);
            if(formFile == null || formFile.Count == 0)
            {
                return Result.Fail<ProductDto>("Atleast One Image Attached To The Product");
            }

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
                if (result == 0)
                {
                    return Result.Fail<ProductDto>("Failed To Create Product");
                }

                var response = new ProductCreateResponseDto()
                {
                    Id = productobj.Id,
                    Description = productobj.Description
                };

                var media = new ProductMedia()
                {
                    ProductId = productobj.Id,
                    Description = productobj.Description
                };

                foreach (var file in formFile)
                {
                    using var stream = file.OpenReadStream();

                    await _mediaService.CreateMedia(media, stream, file.ContentType, file.FileName, ct);

                }

                var currentProduct = await GetProductById(productobj.Id);

                return currentProduct;
            });
        }
        
        public async Task<Result<ProductDto>> UpdateProduct(int ProductId,ProductsUpdateDto productDto,List<IFormFile> formFile,CancellationToken ct)
        {
            var media = new ProductMedia()
            {
                ProductId = ProductId,
                Description = productDto.Description
            };

            foreach (var file in formFile)
            {
                using var stream = file.OpenReadStream();

                await _mediaService.CreateMedia(media, stream, file.ContentType, file.FileName, ct);

            }

            var product = await _context.Products.Include(i=>i.Categories).AsSplitQuery().FirstOrDefaultAsync(i=>i.Id==ProductId);

            if (product == null)
            {
                return Result.Fail<ProductDto>($"The product with Id {ProductId} Not Found");
            }

            if(productDto.CategoriesId!=null && productDto.CategoriesId.Count>0)
            {
                var categories = await _context.Categories
                    .AsNoTracking()
                    .Where(c => productDto.CategoriesId.Contains(c.Id))
                    .ToListAsync();

                product.Categories=categories;
            }


            product.Title = productDto.Title ?? product.Title;
            product.Description = productDto.Description ?? product.Description;
            product.Price = productDto.Price<=0? product.Price:productDto.Price;
            product.Status = productDto.Status;
            product.SpecialStatus = productDto.SpecialStatus;
            product.DisplayOrder = productDto.DisplayOrder ?? product.DisplayOrder;
            

            var result=await _context.SaveChangesAsync(ct);

            if(result==0)
            {
                return Result.Fail<ProductDto>("Failed To Update Product");
            }
            
            await _publish.Publish(new UpdateCartProduct(product.Id, product.Title, product.Price));

            return new ProductDto()
            {
                Id = ProductId,
                Description = product.Description,
                Title = product.Title,
                Price = product.Price,
                Categories = product.Categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Title, Description = c.Description }).ToList()?? new List<CategoryDto>(),
                Media = product.Media.Select(c => new MediaDto { MediaUrl = c.Media }).ToList()??new List<MediaDto>()
            };
        }
        
        public async Task<Result<bool>> DeleteProduct(int productId, CancellationToken ct)
        {
            if(productId<= 0)
            {
                throw new ArgumentException("Product Id is not valid");
            }
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var product = await _context.Products.Include(i => i.Media).Where(I => I.Id == productId).FirstOrDefaultAsync();
                if (product == null)
                {
                    return Result.Fail($"The product with Id {productId} Not Found");
                }
                var media = await Task.WhenAll(product.Media.Select(s => _mediaService.DeleteMedia(s.Media,ct)));
                _context.Products.Remove(product);

                var result = await _context.SaveChangesAsync(ct);
                if(result == 0)
                {
                    return Result.Fail<bool>("There Was An Issue Deleting The Product");
                }
                await _publish.Publish(new DeleteCartProduct(productId));
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
        
        public async Task<List<ProductDto>> ProductSearch(string tag,CancellationToken ct)
        {
           return await _context.Products
                .Include(i=>i.Categories)
                .AsNoTracking()
                .AsSplitQuery()
                .Where(p=>
                    (EF.Functions.TrigramsSimilarity(p.Description,tag)>0.3)
                ||  (EF.Functions.TrigramsSimilarity(p.Title,tag)>0.3)
                ||  (p.Categories.Any(i=>EF.Functions.TrigramsSimilarity(i.Title,tag)>0.3))
                ||  (p.Categories.Any(i => EF.Functions.TrigramsSimilarity(i.Description, tag)>0.3)))
                .Select(i=> new ProductDto
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
                .ToListAsync(ct);
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

        public async Task<Result<bool>> AssignProductToCategory(int productId, int categoryId,CancellationToken ct)
        {
            if(productId<=0 || categoryId<=0)
            {
               throw new ArgumentException("Product or Category isnt valid");
            }

            var product=await _context.Products.Include(p => p.Categories).FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return Result.Fail($"Product with id {productId} not found");
            }

            if (product.Categories.Any(i => i.Id == categoryId))
            {
                return true;
            }

            var category = await _context.Categories.FindAsync(new object[] { categoryId }, ct);
            if (category == null)
            {
                return Result.Fail($"Category with id {categoryId} not found");
            }

            product.Categories.Add(category);

            if (await _context.SaveChangesAsync(ct) == 0)
            {
                return Result.Fail("Failed to assign product to category");
            }
            return true;

        }
    }
}

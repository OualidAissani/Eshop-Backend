using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Catalog.Services
{
    public class ProductRepository: IProductRepository
    {
        private readonly CatalogDbContext _context;
        private readonly IMediaService _mediaService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductRepository> _logger;
        private readonly IConfiguration _configurations;

        public ProductRepository(
            CatalogDbContext context,
            IMediaService mediaService,
            ILogger<ProductRepository> logger
,
            IConfiguration configurations)
        {
            _context = context;
            _mediaService = mediaService;
            _logger = logger;
            _configurations = configurations;
        }
        public class ProductPriceDto
        {
            public int Id { get; set; }
            public double Price { get; set; }
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

        public async Task<dynamic> CreateProduct(ProductCreateDto product)
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
                var result = await _context.SaveChangesAsync();

                return new { productobj.Id, productobj.Description };
            });
        }
        
        public async Task<Products> UpdateProduct(Products product,Stream mediafile,string contentType,string filename)
        {
            if (mediafile != null)
            {
                var media = new ProductMedia()
                {
                    ProductId = product.Id,
                    Description = product.Description,
                };
                await _mediaService.CreateMedia(media,mediafile, contentType, filename);
            }
            _context.Products.Update(product);
            var result=await _context.SaveChangesAsync();
            if(result<=0)
            {
                throw new Exception("Failed to update product");
            }
            var httpClient=_httpClientFactory.CreateClient();
            var response=await httpClient.PutAsJsonAsync($"{_configurations["GatewatUrl"]}/api/Order/Cart", new { ProductId = product.Id, ProductName = product.Title, FullPrice=product.Price});
            response.EnsureSuccessStatusCode();
            return product;
        }
        
        public async Task<bool> DeleteProduct(int productId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var product = await _context.Products.Include(i => i.Media).Where(I => I.Id == productId).FirstOrDefaultAsync();
                if (product == null)
                {
                    return false;
                }
                var media = await Task.WhenAll(product.Media.Select(s => _mediaService.DeleteMedia(s.Media)));
                _context.Products.Remove(product);

                var result = await _context.SaveChangesAsync();
                return true;
            });
        }
        
        public async Task<Products?> GetProductById(int productId)
        {
            return await _context.Products
                .Include(i=>i.Media)
                .Include(c=>c.Categories)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(p => p.Id == productId).FirstOrDefaultAsync();
        }
        
        public async Task<List<Products>> GetAllProducts()
        {
            return await _context.Products
                .Include(i => i.Media)
                .Include(c => c.Categories)
                .AsSplitQuery()
                .AsNoTracking()
                .OrderBy(d => d.DisplayOrder == null ? d.Id : d.DisplayOrder)
                .ToListAsync();            
        }
        
        public async Task<List<Products>> GetProductsByCategory(int categoryId)
        {
            return await _context.Products
                .Include(i => i.Media)
                .Include(c => c.Categories)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(c => c.Categories.Any(cat => cat.Id == categoryId))
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

        public async Task<PaginatedResult<Products>> GetProductsAsync(PaginationParams paging)
        {
            paging.Validate();

            var query = _context.Products;

            int total = await query.CountAsync();

            var items = await query
                .Include(i => i.Media)
                .Include(c => c.Categories)
                .AsSplitQuery()
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Skip((paging.Page - 1) * paging.PageSize)
                .Take(paging.PageSize)
                .ToListAsync();

            return new PaginatedResult<Products>
            {
                Items = items,
                Page = paging.Page,
                PageSize = paging.PageSize,
                Total = total
            };
        }
        
       
    }
}

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
        private readonly ILogger<ProductRepository> _logger;
        
        public ProductRepository(
            CatalogDbContext context,
            IMediaService mediaService,
            ILogger<ProductRepository> logger
            ) 
        { 
            _context = context;
            _mediaService = mediaService;
            _logger = logger;
        }
        
        public async Task<double> GetProductPrice(int ProductId)
        {
            return await _context.Products.Where(i=>i.Id==ProductId).Select(i => i.Price).FirstOrDefaultAsync();
        }

        public async Task<dynamic> CreateProduct(ProductCreateDto product)
        {
            var transaction=await _context.Database.BeginTransactionAsync();
            try
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
                await transaction.CommitAsync();
                
                return new { productobj.Id, productobj.Description };
            }
            catch(Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
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
            
           
            
            return product;
        }
        
        public async Task<bool> DeleteProduct(int productId)
        {
            var transaction=await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.Include(i => i.Media).Where(I => I.Id == productId).FirstOrDefaultAsync();
                if (product == null)
                {
                    return false;
                }
                var media = product.Media.Select(s => _mediaService.DeleteMedia(s.Media));
                _context.Products.Remove(product);

                var result = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch(Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
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

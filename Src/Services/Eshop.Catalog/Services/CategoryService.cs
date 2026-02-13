using Eshop.Catalog.Data;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Catalog.Services
{
    public class CategoryService:ICategoryService
    {
        private readonly CatalogDbContext _context;
        private readonly ILogger<CategoryService> _logger;
        public CategoryService(CatalogDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Categories> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken )
        {

            var category=new Categories()
            {
                Title=dto.Title,
                Description=dto.Description,
                
            };
            _context.Categories.Add(category);
            if(await _context.SaveChangesAsync(cancellationToken) == 0)
            {
                _logger.LogError("Db Returned 0 rows effected");
                return null;
            }

            return category;
        }


        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken )
        {
            var category= await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogError("Category with id {id} not found", id);
                return false;
            }
            _context.Categories.Remove(category);
            if(await _context.SaveChangesAsync(cancellationToken) == 0)
            {
                _logger.LogError("0 rows effected");
                return false;
            }
            return true;
        }

        public async Task<List<Categories>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Categories.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<Categories> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Categories.AsNoTracking().FirstOrDefaultAsync(i=>i.Id==id,cancellationToken);
        }

        public async Task<Categories> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken)
        {
            var category= await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogError("Category with id {id} not found", id);
                return null;
            }
            if (dto == null) {

                _logger.LogError("Category update dto is null");
                return null;
            }
            if (dto.Title != null)
            {
                category.Title = dto.Title;
            }
            if (dto.Description != null) {

                category.Description=dto.Description;
            }
            if(await _context.SaveChangesAsync(cancellationToken) == 0)
            {
                _logger.LogError("");
                return null;
            }
            return category;
        }
    }
}

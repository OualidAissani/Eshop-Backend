using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using FluentResults;
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

        public async Task<Result<Categories>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken )
        {
            ArgumentNullException.ThrowIfNull(dto);
            var category=new Categories()
            {
                Title=dto.Title,
                Description=dto.Description,
                
            };
            _context.Categories.Add(category);
            if(await _context.SaveChangesAsync(cancellationToken) == 0)
            {
                return Result.Fail("Error Creating Category Try Again Later");
            }

            return category;
        }


        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken )
        {
            var category= await _context.Categories.FindAsync(id);

            ArgumentNullException.ThrowIfNull(category);

            _context.Categories.Remove(category);
            if(await _context.SaveChangesAsync(cancellationToken) == 0)
            {
                return Result.Fail("Error Deleting Category Try Again Later");
            }
            return true;
        }

        public async Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Categories.AsNoTracking().Select(i=> new CategoryDto {
                Id= i.Id,
                Description= i.Description,
                Name= i.Title
            }).ToListAsync(cancellationToken);
        }

        public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Categories.AsNoTracking().Select(i=>new CategoryDto {
                Id= i.Id,
                Description = i.Description,
                Name = i.Title
            }).FirstOrDefaultAsync(i=>i.Id==id,cancellationToken);
        }

        public async Task<Result<Categories>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken)
        {

            ArgumentNullException.ThrowIfNull(dto);
            if(id<=0) return Result.Fail("Invalid Id");

            var category = await _context.Categories.FindAsync(id);

            if(category==null) return Result.Fail("Category Not Found");


            if (dto.Title != null)
            {
                category.Title = dto.Title;
            }
            if (dto.Description != null) {

                category.Description=dto.Description;
            }
            if(await _context.SaveChangesAsync(cancellationToken) == 0)
            {
                return Result.Fail("Error Updating Category Try Again Later");
            }
            return category;
        }
    }
}

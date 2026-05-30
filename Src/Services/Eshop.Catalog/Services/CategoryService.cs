using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using FluentResults;
using MongoDB.Driver;

namespace Eshop.Catalog.Services
{
    public class CategoryService:ICategoryService
    {
        private readonly MongoCatalogContext _context;
        private readonly ILogger<CategoryService> _logger;
        public CategoryService(MongoCatalogContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<Categories>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken )
        {
            if (dto is null)
            {
                return Result.Fail<Categories>("Invalid category payload");
            }

            var categoryId = await GetNextCategoryId(cancellationToken);
            var category = new CategoryDocument
            {
                CategoryId = categoryId,
                Title = dto.Title,
                Description = dto.Description
            };

            await _context.Categories.InsertOneAsync(category, cancellationToken: cancellationToken);

            return new Categories
            {
                Id = category.CategoryId,
                Title = category.Title,
                Description = category.Description
            };
        }


        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct )
        {
            if(id<=0)
                {
                   throw new ArgumentOutOfRangeException(nameof(id), "Id must be greater than zero.");
            }
            var category = await _context.Categories.Find(i => i.CategoryId == id).FirstOrDefaultAsync(ct);

            if (category == null)
            {
                return Result.Fail<bool>("Category Not Found");
            }

            var deleteResult = await _context.Categories.DeleteOneAsync(i => i.CategoryId == id, ct);
            if (deleteResult.DeletedCount == 0)
            {
                return Result.Fail<bool>("Error Deleting Category Try Again Later");
            }
            return true;
        }

        public async Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            var categories = await _context.Categories
                .Find(FilterDefinition<CategoryDocument>.Empty)
                .ToListAsync(cancellationToken);

            return categories.Select(i => new CategoryDto
            {
                Id = i.CategoryId,
                Description = i.Description,
                Name = i.Title
            }).ToList();
        }

        public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .Find(i => i.CategoryId == id)
                .FirstOrDefaultAsync(cancellationToken);

            return category == null
                ? null
                : new CategoryDto
                {
                    Id = category.CategoryId,
                    Description = category.Description,
                    Name = category.Title
                };
        }

        public async Task<Result<Categories>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken)
        {

            ArgumentNullException.ThrowIfNull(dto);
            if(id<=0) return Result.Fail("Invalid Id");

            var category = await _context.Categories.Find(i => i.CategoryId == id).FirstOrDefaultAsync(cancellationToken);

            if(category==null) return Result.Fail("Category Not Found");

            category.Title = dto.Title ?? category.Title;
            category.Description = dto.Description ?? category.Description;

            var updateResult = await _context.Categories.ReplaceOneAsync(
                i => i.CategoryId == id,
                category,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken);

            if (updateResult.ModifiedCount == 0 && updateResult.MatchedCount == 0)
            {
                return Result.Fail("Error Updating Category Try Again Later");
            }

            return new Categories
            {
                Id = category.CategoryId,
                Title = category.Title,
                Description = category.Description
            };
        }

        private async Task<int> GetNextCategoryId(CancellationToken ct)
        {
            var update = Builders<CounterDocument>.Update.Inc(c => c.Value, 1);
            var options = new FindOneAndUpdateOptions<CounterDocument, CounterDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var counter = await _context.Counters.FindOneAndUpdateAsync<CounterDocument, CounterDocument>(
                c => c.Name == "categories",
                update,
                options,
                ct);

            return counter.Value;
        }
    }
}

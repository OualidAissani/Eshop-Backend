using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Services;
using FluentAssertions;
using Imposter.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Xunit;

[assembly: GenerateImposter(typeof(ILogger<>))]

namespace Eshop.Test
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly CategoryService _sut;
        private readonly CatalogDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        private readonly ILoggerImposter<CategoryService> _loggerImposter;

        public CategoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new CatalogDbContext(options);

            _loggerImposter = ILogger<CategoryService>.Imposter();
            _logger = _loggerImposter.Instance();

            _sut = new CategoryService(_context, _logger);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task CreateAsync_ValidDto_SaveChangesAndReturnsIt()
        {
            var dto = new CategoryCreateDto
            {
                Title = "Title",
                Description = "Description",
            };

            var result = await _sut.CreateAsync(dto, CancellationToken.None);

            var returned = await _sut.GetByIdAsync(result.Value.Id, CancellationToken.None);

            returned.Should().NotBeNull();
            result.Errors.Any().Should().BeFalse();
            result.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_InValidDto_ResultFailed()
        {
            var result = await _sut.CreateAsync(null, CancellationToken.None);
            result.Errors.Any().Should().BeTrue();
            result.ValueOrDefault.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ValidCategory_SaveChangesAndReturnsIt()
        {
            var category = new Eshop.Catalog.Models.Categories
            {
                Title = "Title",
                Description = "Description",
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            var dto = new CategoryUpdateDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
            };
            var result = await _sut.UpdateAsync(category.Id, dto, CancellationToken.None);
            var returned = await _sut.GetByIdAsync(result.Value.Id, CancellationToken.None);
            returned.Should().NotBeNull();
            result.Errors.Any().Should().BeFalse();
            result.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_InValidCategory_ResultFailed()
        {
            var dto = new CategoryUpdateDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
            };
            var result = await _sut.UpdateAsync(999, dto, CancellationToken.None);
            result.Errors.Any().Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_ValidCategory_SaveChangesAndReturnsTrue()
        {
            var category = new Eshop.Catalog.Models.Categories
            {
                Title = "Title",
                Description = "Description",
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            var result = await _sut.DeleteAsync(category.Id, CancellationToken.None);
            var returned = await _sut.GetByIdAsync(category.Id, CancellationToken.None);
            returned.Should().BeNull();
            result.Errors.Any().Should().BeFalse();
            result.Value.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_InValidCategory_ResultFailed()
        {
            var result = await _sut.DeleteAsync(999, CancellationToken.None);
            result.Errors.Any().Should().BeTrue();
            result.IsFailed.Should().BeTrue();
            result.ValueOrDefault.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_ValidCategory_ReturnsIt()
        {
            var category = new Eshop.Catalog.Models.Categories
            {
                Title = "Title",
                Description = "Description",
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            var result = await _sut.GetByIdAsync(category.Id, CancellationToken.None);
            result.Should().NotBeNull();
            result.Id.Should().Be(category.Id);
        }

        [Fact]
        public async Task GetByIdAsync_InValidCategory_ReturnsNull()
        {
            var result = await _sut.GetByIdAsync(999, CancellationToken.None);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllCategories()
        {
            var category1 = new Eshop.Catalog.Models.Categories
            {
                Title = "Title1",
                Description = "Description1",
            };
            var category2 = new Eshop.Catalog.Models.Categories
            {
                Title = "Title2",
                Description = "Description2",
            };
            _context.Categories.AddRange(category1, category2);
            await _context.SaveChangesAsync();
            var result = await _sut.GetAllAsync(CancellationToken.None);
            result.Should().NotBeNull();
            result.Count.Should().Be(2);
        }
        
    }
}

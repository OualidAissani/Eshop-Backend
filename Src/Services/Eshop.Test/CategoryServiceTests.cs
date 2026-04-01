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
            var options=new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new CatalogDbContext(options);

             _loggerImposter = ILogger<CategoryService>.Imposter();
            _logger = _loggerImposter.Instance();

            _sut = new CategoryService(_context, _logger);
        }

        public  void Dispose() =>  _context.Dispose();

        [Fact]
        public async Task CreateAsync_ValidDto_SaveChangesAndReturnsIt()
        {
            var dto = new CategoryCreateDto
            {
                Title = "Title",
                Description = "Description",
            };

            var result=await _sut.CreateAsync(dto,CancellationToken.None);

            var returned = await _sut.GetByIdAsync(result.Value.Id,CancellationToken.None);

            returned.Should().NotBeNull();
            result.Errors.Any().Should().BeFalse();
            result.Value.Should().NotBeNull();
        }


    }
}

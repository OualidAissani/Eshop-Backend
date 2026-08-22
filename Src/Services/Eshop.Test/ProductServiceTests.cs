using Eshop.Catalog.Data;
using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services;
using Eshop.Catalog.Services.IServices;
using Eshop.Events;
using Eshop.Payment.Data;
using FluentAssertions;
using Imposter.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using MongoDB.Driver;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using Xunit;
using Eshop.Catalog.Data.Enums;

[assembly:GenerateImposter(typeof(ILogger<>))]
[assembly:GenerateImposter(typeof(IHttpClientFactory))]
[assembly:GenerateImposter(typeof(IPublishEndpoint))]

namespace Eshop.Test
{
    public class ProductServiceTests : IDisposable
    {
        private readonly ProductService _sut;
        private readonly CatalogDbContext _context;
        private readonly MongoCatalogContext _mongoContext;
        private readonly ILogger<ProductService> _logger;
        private readonly IConfiguration _configurations;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPublishEndpoint _publish;
        private readonly IMediaService _mediaService;
        private readonly FakeHttpMessageHandler _handler;


        private readonly ILoggerImposter<ProductService> _loggerImposter;
        private readonly IPublishEndpointImposter _publishEndpointImposter;
        private readonly IHttpClientFactoryImposter _httpClientFactoryImposter;

        public ProductServiceTests()
        {
            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new CatalogDbContext(options);

            _handler = new FakeHttpMessageHandler();
            _handler.When(HttpMethod.Post, "upload.uploadcare.com", () =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"file":"abc-123-uuid"}""")
                });

            _handler.When(HttpMethod.Delete, "api.uploadcare.com", () =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"status":"success"}""")
                });

            _httpClientFactoryImposter = IHttpClientFactory.Imposter();
            _httpClientFactoryImposter.CreateClient(Arg<string>.Any()).Returns(new HttpClient(_handler));
            _httpClientFactory = _httpClientFactoryImposter.Instance();

            _configurations = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UploadCare:PublicKey"] = "test_public_key",
                    ["UploadCare:Store"] = "1",
                    ["UploadCare:SecretKey"] = "test_secret_key",
                    ["UploadCare:UploadCareBaseUrl"] = "https://ucarecdn.com/"
                })
                .Build();

            _mediaService = new MediaService(_httpClientFactory, _configurations);

            _loggerImposter = ILogger<ProductService>.Imposter();
            _logger = _loggerImposter.Instance();

            _publishEndpointImposter = IPublishEndpoint.Imposter();
            _publish = _publishEndpointImposter.Instance();

            var mongoClient = new MongoClient("mongodb://localhost:27017");
            var mongoSettings = Microsoft.Extensions.Options.Options.Create(new MongoSettings
            {
                Database = $"test-{Guid.NewGuid()}",
                ProductsCollection = "products",
                CategoriesCollection = "categories",
                CountersCollection = "counters"
            });
            _mongoContext = new MongoCatalogContext(mongoClient, mongoSettings);
            _sut = new ProductService(_mongoContext, _mediaService, _logger, _configurations, _httpClientFactory, _publish);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CreateProduct_Success_ReturnSucessWithProduct()
        {
            ProductCreateDto productCreateDto;
            List<IFormFile> files;
            ProductCreateDtoSetup(out productCreateDto, out files);

            var productCreateResult = await _sut.CreateProduct(productCreateDto, files, CancellationToken.None);

            var fetchedProduct = _mongoContext.Products.AsQueryable().FirstOrDefault(p => p.ProductId == productCreateResult.Value.Id);

            productCreateResult.IsSuccess.Should().BeTrue();
            fetchedProduct.Should().NotBeNull();
            fetchedProduct.Description.Should().BeEquivalentTo(productCreateDto.Description);
            fetchedProduct.Title.Should().BeEquivalentTo(productCreateDto.Title);
            fetchedProduct.Media.Should().HaveCount(1);

        }

        private static void ProductCreateDtoSetup(out ProductCreateDto productCreateDto, out List<IFormFile> files)
        {
            productCreateDto = new ProductCreateDto
            {
                Title = "Test",
                Description = "TestdESC",
                SpecialStatus = ProductSpecialStatus.New,
                Status = ProductStatus.Available,
                Price = 911,
            };
            var fileBytes = Encoding.UTF8.GetBytes("fake-image-content");
            var stream = new MemoryStream(fileBytes);

            IFormFile formFile = new FormFile(stream, 0, fileBytes.Length, "formFile", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            files = new List<IFormFile> { formFile };
        }

        [Fact]
        public async Task CreateProduct_NotMediaAttached_ReturnFailedWithError()
        {
            var productCreateDto = new ProductCreateDto
            {
                Title = "Test",
                Description = "TestdESC",
                SpecialStatus = ProductSpecialStatus.New,
                Status = ProductStatus.Available,
                Price = 911,
            };

            var productCreateResult = await _sut.CreateProduct(productCreateDto, null, CancellationToken.None);

            productCreateResult.IsFailed.Should().BeTrue();
            productCreateResult.Errors.First().Message.Should().Be("Atleast One Image Attached To The Product");

        }

        [Fact]
        public async Task UpdateProduct_Success_ReturnUpdatedProduct()
        {
            ProductCreateDto productCreateDto;
            List<IFormFile> files;
            ProductCreateDtoSetup(out productCreateDto, out files);


            var productCreateResult = await _sut.CreateProduct(productCreateDto, files, CancellationToken.None);

            var updatedDto = new ProductsUpdateDto
            {
                Title = "Updated Test",
                Description = "Updated Description",
                SpecialStatus = ProductSpecialStatus.LimitedEdition,
                Status = ProductStatus.Available,
                Price = 1500,
            };

            var updateResult = await _sut.UpdateProduct(productCreateResult.Value.Id, updatedDto, files, CancellationToken.None);

            var fetchedResult = _mongoContext.Products.AsQueryable().FirstOrDefault(p => p.ProductId == productCreateResult.Value.Id);

            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Errors.Should().BeEmpty();

            _publishEndpointImposter
                .Publish(Arg<UpdateCartProduct>.Is(s =>
                s.ProductId == productCreateResult.Value.Id &&
                s.ProductName == updatedDto.Title &&
                s.FullPrice == updatedDto.Price),
            Arg<CancellationToken>.Any())
            .Called(Count.Once());

            
        }

        [Fact]
        public async Task UpdateProduct_FailedUpdate_ReturnFailedWithError()
        {
            ProductCreateDto productCreateDto;
            List<IFormFile> files;
            ProductCreateDtoSetup(out productCreateDto, out files);
            var updatedDto = new ProductsUpdateDto
            {
                Title = "Updated Test",
                Description = "Updated Description",
                SpecialStatus = ProductSpecialStatus.LimitedEdition,
                Status = ProductStatus.Available,
                Price = 1500,
            };
            var updateResult = await _sut.UpdateProduct(12, updatedDto, files, CancellationToken.None);
            updateResult.IsFailed.Should().BeTrue();
            updateResult.Errors.First().Message.Should().Be("The product with Id 12 Not Found");
        }

        [Fact]
        public async Task DeleteProduct_Success_ReturnSuccess()
        {
            ProductCreateDto productCreateDto;
            List<IFormFile> files;
            ProductCreateDtoSetup(out productCreateDto, out files);
            var productCreateResult = await _sut.CreateProduct(productCreateDto, files, CancellationToken.None);
            var deleteResult = await _sut.DeleteProduct(productCreateResult.Value.Id, CancellationToken.None);

            var fetchedResult = _mongoContext.Products.AsQueryable().FirstOrDefault(i => i.ProductId == productCreateResult.Value.Id);

            fetchedResult.Should().BeNull();
            deleteResult.IsSuccess.Should().BeTrue();
            deleteResult.Errors.Should().BeEmpty();

            _publishEndpointImposter
                .Publish(Arg<DeleteCartProduct>.Is(s =>
                s.ProductId == productCreateResult.Value.Id),
            Arg<CancellationToken>.Any())
            .Called(Count.Once());
        }

        [Fact]
        public async Task DeleteProduct_ProductDoesntExist_ReturnFailedWithError()
        {
            var deleteResult = await _sut.DeleteProduct(12, CancellationToken.None);
            deleteResult.IsFailed.Should().BeTrue();
            deleteResult.Errors.First().Message.Should().Be("The product with Id 12 Not Found");
        }

        [Fact(Skip = "trigrams wont work for in-memory database")]
        public async Task ProductSearch_CloseSimiliarity_ReturnedExpectedProduct()
        {
            var product1 = new ProductDocument
            {
                ProductId = 1,
                Title = "Apple iPhone 13",
                Description = "Latest model of iPhone with A15 Bionic chip",
                Price = 999,
                Status = ProductStatus.Available,
                SpecialStatus = ProductSpecialStatus.New
            };
            var product2 = new ProductDocument
            {
                ProductId = 2,
                Title = "Samsung Galaxy S21",
                Description = "Flagship Samsung phone with Exynos 2100",
                Price = 799,
                Status = ProductStatus.Available,
                SpecialStatus = ProductSpecialStatus.New
            };
            await _mongoContext.Products.InsertManyAsync(new[] { product1, product2 });
            var searchResult = await _sut.ProductSearch("iPhone", CancellationToken.None);

            searchResult.Should().HaveCount(1);
            searchResult.First().Title.Should().Be("Apple iPhone 13");
        }

    }
}

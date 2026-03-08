using Eshop.Catalog.Data;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services;
using FluentAssertions;
using Imposter.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using Xunit;

namespace Eshop.Test;

public class MediaServiceTests : IDisposable
{
    private readonly CatalogDbContext _context;
    private readonly FakeHttpMessageHandler _handler;
    private readonly MediaService _sut;

    public MediaServiceTests()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new CatalogDbContext(options);

        _handler = new FakeHttpMessageHandler();

        var factoryImposter = IHttpClientFactory.Imposter();
        factoryImposter.CreateClient(Arg<string>.Any()).Returns(new HttpClient(_handler));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UploadCare:PublicKey"] = "test_public_key",
                ["UploadCare:Store"] = "1",
                ["UploadCare:SecretKey"] = "test_secret_key",
                ["UploadCare:UploadCareBaseUrl"] = "https://ucarecdn.com/"
            })
            .Build();

        _sut = new MediaService(_context, factoryImposter.Instance(), config);
    }

    public void Dispose() => _context.Dispose();

    #region CreateMedia

    [Fact]
    public async Task CreateMedia_UploadsFileAndPersistsToDatabase()
    {
        _handler.When(HttpMethod.Post, "upload.uploadcare.com", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"file":"abc-123-uuid"}""")
            });

        var media = new ProductMedia { ProductId = 1, Description = "Test image" };
        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);

        var result = await _sut.CreateMedia(media, stream, "image/png", "test.png", CancellationToken.None);

        result.Media.Should().Be("https://ucarecdn.com/abc-123-uuid");

        var saved = await _context.Media.FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.Media.Should().Be("https://ucarecdn.com/abc-123-uuid");
        saved.ProductId.Should().Be(1);
        saved.Description.Should().Be("Test image");
    }

    [Fact]
    public async Task CreateMedia_ReturnsSameMediaObjectWithUpdatedUrl()
    {
        _handler.When(HttpMethod.Post, "upload.uploadcare.com", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"file":"xyz-uuid"}""")
            });

        var media = new ProductMedia { ProductId = 2, Description = "Product shot" };
        using var stream = new MemoryStream([0x01]);

        var result = await _sut.CreateMedia(media, stream, "image/jpeg", "photo.jpg", CancellationToken.None);

        result.Should().BeSameAs(media);
        result.Media.Should().Be("https://ucarecdn.com/xyz-uuid");
    }

    [Fact]
    public async Task CreateMedia_WhenUploadcareReturnsError_ThrowsHttpRequestException()
    {
        _handler.When(HttpMethod.Post, "upload.uploadcare.com", () =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        var media = new ProductMedia { ProductId = 1, Description = "Test image" };
        using var stream = new MemoryStream([0x01]);

        var act = async () => await _sut.CreateMedia(media, stream, "image/png", "fail.png", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreateMedia_WhenUploadFails_DoesNotPersistToDatabase()
    {
        _handler.When(HttpMethod.Post, "upload.uploadcare.com", () =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var media = new ProductMedia { ProductId = 1, Description = "Test" };
        using var stream = new MemoryStream([0x01]);

        try { await _sut.CreateMedia(media, stream, "image/png", "fail.png", CancellationToken.None); } catch { }

        var count = await _context.Media.CountAsync();
        count.Should().Be(0);
    }

    #endregion

    #region DeleteMedia

    [Fact]
    public async Task DeleteMedia_WhenUuidIsNull_ReturnsFalse()
    {
        var result = await _sut.DeleteMedia(null!, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMedia_WhenUuidIsEmpty_ReturnsFalse()
    {
        var result = await _sut.DeleteMedia(string.Empty, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMedia_WhenUuidIsWhitespace_ReturnsFalse()
    {
        var result = await _sut.DeleteMedia("   ", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMedia_WhenApiReturnsSuccess_ReturnsTrue()
    {
        _handler.When(HttpMethod.Delete, "api.uploadcare.com/files/storage", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"ok"}""")
            });

        var result = await _sut.DeleteMedia("abc-123-uuid", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMedia_WhenApiReturnsUnauthorized_ReturnsFalse()
    {
        _handler.When(HttpMethod.Delete, "api.uploadcare.com/files/storage", () =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"detail":"Authentication credentials were not provided."}""")
            });

        var result = await _sut.DeleteMedia("abc-123-uuid", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMedia_WhenApiReturnsNotFound_ReturnsFalse()
    {
        _handler.When(HttpMethod.Delete, "api.uploadcare.com/files/storage", () =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _sut.DeleteMedia("nonexistent-uuid", CancellationToken.None);

        result.Should().BeFalse();
    }

    #endregion
}

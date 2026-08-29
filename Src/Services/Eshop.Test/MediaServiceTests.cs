using Eshop.Catalog.Entities;
using Eshop.Catalog.Services;
using FluentAssertions;
using Imposter.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net;
using Xunit;

namespace Eshop.Test;

public class MediaServiceTests : IDisposable
{
    private readonly FakeHttpMessageHandler _handler;
    private readonly MediaService _sut;

    public MediaServiceTests()
    {
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

        _sut = new MediaService(factoryImposter.Instance(), config);
    }

    public void Dispose() { }

    #region CreateMedia

    [Fact]
    public async Task CreateMedia_UploadsFileAndReturnsMedia()
    {
        _handler.When(HttpMethod.Post, "upload.uploadcare.com", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"file":"abc-123-uuid"}""")
            });

        var media = new ProductMediaItem { Description = "Test image" };
        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);

        var result = await _sut.CreateMedia(media, stream, "image/png", "test.png", CancellationToken.None);

        result.Media.Should().Be("https://ucarecdn.com/abc-123-uuid");

        result.Should().NotBeNull();
        result.Media.Should().Be("https://ucarecdn.com/abc-123-uuid");
        result.Description.Should().Be("Test image");
    }

    [Fact]
    public async Task CreateMedia_ReturnsSameMediaObjectWithUpdatedUrl()
    {
        _handler.When(HttpMethod.Post, "upload.uploadcare.com", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"file":"xyz-uuid"}""")
            });

        var media = new ProductMediaItem { Description = "Product shot" };
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

        var media = new ProductMediaItem { Description = "Test image" };
        using var stream = new MemoryStream([0x01]);

        var act = async () => await _sut.CreateMedia(media, stream, "image/png", "fail.png", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreateMedia_WhenUploadFails_DoesNotReturnMedia()
    {
        _handler.When(HttpMethod.Post, "upload.uploadcare.com", () =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var media = new ProductMediaItem { Description = "Test" };
        using var stream = new MemoryStream([0x01]);

        try { await _sut.CreateMedia(media, stream, "image/png", "fail.png", CancellationToken.None); } catch { }

        media.Media.Should().BeNull();
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

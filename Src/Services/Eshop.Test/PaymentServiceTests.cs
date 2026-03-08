using Eshop.Payment.Data;
using Eshop.Payment.Services;
using PaymentModels = Eshop.Payment.Models;
using FluentAssertions;
using Imposter.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;
using Xunit;

[assembly: GenerateImposter(typeof(IHttpClientFactory))]

namespace Eshop.Test;

public class PaymentServiceTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly FakeHttpMessageHandler _handler;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new PaymentDbContext(options);

        _handler = new FakeHttpMessageHandler();
        _handler.When(HttpMethod.Post, "oauth2/token", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"access_token":"fake_token"}""")
            });

        var factoryImposter = IHttpClientFactory.Imposter();
        factoryImposter.CreateClient(Arg<string>.Any()).Returns(new HttpClient(_handler));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Paypal:ClientId"] = "test_client_id",
                ["Paypal:SecretKey"] = "test_secret_key"
            })
            .Build();

        _sut = new PaymentService(
            factoryImposter.Instance(),
            config,
            _context,
            NullLogger<PaymentService>.Instance,
            Substitute.For<IPublishEndpoint>());
    }

    public void Dispose() => _context.Dispose();

    #region GetAccessToken

    [Fact]
    public async Task GetAccessToken_ReturnsToken()
    {
        var token = await _sut.GetAccessToken();

        token.Should().Be("fake_token");
    }

    #endregion

    #region CreateOrder

    [Fact]
    public async Task CreateOrder_ReturnsPayerActionUrl()
    {
        _handler.When(HttpMethod.Post, "v2/checkout/orders", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                    "id": "ORDER123",
                    "links": [
                        {"rel": "self", "href": "https://api.paypal.com/self"},
                        {"rel": "payer-action", "href": "https://paypal.com/checkoutnow?token=ORDER123"}
                    ]
                }
                """)
            });

        var result = await _sut.CreateOrder(
            [new PaymentModels.ItemsDto { name = "Widget", quantity = 1, description = "Test", unit_amount = new PaymentModels.AmountDto { value = "10.00" } }],
            new PaymentModels.AmountDto { value = "10.00", currency_code = "USD" },
            orderId: 1,
            correlationId: Guid.NewGuid().ToString());

        result.Should().Contain("paypal.com");
    }

    #endregion

    #region CapturePayment

    [Fact]
    public async Task CapturePayment_WhenApproved_SavesPaymentAndReturnsNonZero()
    {
        _handler.When(HttpMethod.Get, "v2/checkout/orders/", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"APPROVED"}""")
            });

        _handler.When(HttpMethod.Post, "/capture", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                    "purchase_units": [{
                        "payments": {
                            "captures": [{
                                "id": "CAP123",
                                "status": "COMPLETED",
                                "amount": {"value": "50.00", "currency_code": "USD"}
                            }]
                        }
                    }]
                }
                """)
            });

        var correlationId = Guid.NewGuid().ToString();
        var result = await _sut.CapturePayment("ORDER_TOKEN", "user1", orderSagaId: 1, correlationId);

        result.Should().BeGreaterThan(0);

        var payment = await _context.Payments.FirstOrDefaultAsync();
        payment.Should().NotBeNull();
        payment!.CaptureId.Should().Be("CAP123");
        payment.Status.Should().Be(Status.Captured);
        payment.Amount.Should().Be(50.00m);
    }

    [Fact]
    public async Task CapturePayment_WhenNotApproved_ReturnsZero()
    {
        _handler.When(HttpMethod.Get, "v2/checkout/orders/", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"CREATED"}""")
            });

        var result = await _sut.CapturePayment("ORDER_TOKEN", "user1", orderSagaId: 1, Guid.NewGuid().ToString());

        result.Should().Be(0);
    }

    #endregion

    #region RefundPayment

    [Fact]
    public async Task RefundPayment_WhenValid_UpdatesStatusToRefunded()
    {
        _context.Payments.Add(new Eshop.Payment.Models.Payment
        {
            Id = Guid.NewGuid(),
            CaptureId = "CAP_REFUND",
            OrderId = "1",
            Status = Status.Captured,
            Amount = 100m,
            CapturedAt = DateTime.UtcNow,
            UserId = "user1"
        });
        await _context.SaveChangesAsync();

        _handler.When(HttpMethod.Post, "/refund", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"COMPLETED","id":"REFUND123"}""")
            });

        var result = await _sut.RefundPayment("CAP_REFUND", new AmountDto { value = 50.00m }, "user1");

        result.Should().NotBeNull();
        var payment = await _context.Payments.FirstAsync(p => p.CaptureId == "CAP_REFUND");
        payment.Status.Should().Be(Status.Refunded);
    }

    [Fact]
    public async Task RefundPayment_WhenNullInputs_ReturnsNull()
    {
        var result = await _sut.RefundPayment(null!, null, null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RefundPayment_WhenUserMismatch_ReturnsNull()
    {
        _context.Payments.Add(new Eshop.Payment.Models.Payment
        {
            Id = Guid.NewGuid(),
            CaptureId = "CAP_OTHER",
            OrderId = "1",
            Status = Status.Captured,
            Amount = 100m,
            CapturedAt = DateTime.UtcNow,
            UserId = "user1"
        });
        await _context.SaveChangesAsync();

        var result = await _sut.RefundPayment("CAP_OTHER", new AmountDto { value = 50.00m }, "wrong_user");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RefundPayment_WhenPaymentNotFound_ReturnsNull()
    {
        var result = await _sut.RefundPayment("NONEXISTENT", new AmountDto { value = 50.00m }, "user1");

        result.Should().BeNull();
    }

    #endregion

    #region GetOrderDetails

    [Fact]
    public async Task GetOrderDetails_ReturnsDeserializedJson()
    {
        _handler.When(HttpMethod.Get, "v2/checkout/orders/", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"ORD1","status":"COMPLETED"}""")
            });

        var result = await _sut.GetOrderDetails("ORD1");

        result.GetProperty("status").GetString().Should().Be("COMPLETED");
    }

    #endregion

    #region GetCaptureDetails

    [Fact]
    public async Task GetCaptureDetails_ReturnsDeserializedJson()
    {
        _handler.When(HttpMethod.Get, "v2/payments/captures/", () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"CAP1","status":"COMPLETED"}""")
            });

        var result = await _sut.GetCaptureDetails("CAP1");

        result.GetProperty("status").GetString().Should().Be("COMPLETED");
    }

    #endregion
}

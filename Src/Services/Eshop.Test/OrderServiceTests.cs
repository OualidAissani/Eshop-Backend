using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.Models;
using Eshop.Orders.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Eshop.Test;

public class OrderServiceTests : IDisposable
{
    private readonly OrderDbContext _context;
    private readonly IRequestClient<GetProductRequest> _productClient;
    private readonly IRequestClient<ProductInventoryAvailibityForOrderRequest> _inventoryClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        _productClient = Substitute.For<IRequestClient<GetProductRequest>>();
        _inventoryClient = Substitute.For<IRequestClient<ProductInventoryAvailibityForOrderRequest>>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();

        _sut = new OrderService(_context, _productClient, _inventoryClient, _httpContextAccessor, _publishEndpoint);
    }

    public void Dispose() => _context.Dispose();

    #region GetAllOrders

    [Fact]
    public async Task GetAllOrders_ReturnsAllOrders()
    {
        _context.Orders.AddRange(
            CreateOrder(userId: "user1", totalPrice: 100m),
            CreateOrder(userId: "user2", totalPrice: 200m));
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllOrders();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllOrders_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _sut.GetAllOrders();

        result.Should().BeEmpty();
    }

    #endregion

    #region GetAllUserOrderAsync

    [Fact]
    public async Task GetAllUserOrderAsync_ReturnsOnlyUserOrders()
    {
        _context.Orders.AddRange(
            CreateOrder(userId: "user1", totalPrice: 50m),
            CreateOrder(userId: "user1", totalPrice: 75m),
            CreateOrder(userId: "user2", totalPrice: 120m));
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllUserOrderAsync("user1");

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.UserId.Should().Be("user1"));
    }

    [Fact]
    public async Task GetAllUserOrderAsync_WhenNoOrders_ReturnsEmptyList()
    {
        var result = await _sut.GetAllUserOrderAsync("nonexistent");

        result.Should().BeEmpty();
    }

    #endregion

    #region GetOrderById

    [Fact]
    public async Task GetOrderById_WhenOrderExists_ReturnsOrder()
    {
        var order = CreateOrder(userId: "user1", totalPrice: 99.99m);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _sut.GetOrderById(order.Id, "user1");

        result.Should().NotBeNull();
        result!.TotalPrice.Should().Be(99.99m);
    }

    [Fact]
    public async Task GetOrderById_WhenOrderNotFound_ReturnsNull()
    {
        var result = await _sut.GetOrderById(999, "user1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrderById_WhenUserIdDoesNotMatch_ReturnsNull()
    {
        var order = CreateOrder(userId: "user1", totalPrice: 50m);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _sut.GetOrderById(order.Id, "wrongUser");

        result.Should().BeNull();
    }

    #endregion

    #region CreateOrder

    [Fact]
    public async Task CreateOrder_WithCashOnDelivery_CreatesOrderAndPublishesEvent()
    {
        var orderDto = new OrderDto
        {
            Products = [new OrderItemDto { ProductId = 1, Quantity = 2 }],
            PayementMethod = "CashOnDelivery",
            ShippingAddress = "123 Main St",
            UserId = "user1"
        };

        SetupImpostorResponses(
            inventoryItems: [new ProductInventoryItem(1, 10, 100)],
            products: [new GetProductResponseDto(1, 25.00m, "Widget")]);

        var result = await _sut.CreateOrder(orderDto, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalPrice.Should().Be(50.00m);
        result.OrderItems.Should().HaveCount(1);
        result.OrderItems[0].ProductName.Should().Be("Widget");
        result.OrderItems[0].UnitPrice.Should().Be(25.00m);
        result.OrderItems[0].FullPrice.Should().Be(50.00m);

        await _publishEndpoint.Received(1)
            .Publish(Arg.Is<OrderSubmitted>(e =>
                e.OrderId == result.Id &&
                e.PaymentMethod == Events.PaymentMethods.CashOnDelivery),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrder_WithCreditCard_PublishesEventWithPaymentItems()
    {
        var orderDto = new OrderDto
        {
            Products = [new OrderItemDto { ProductId = 1, Quantity = 1 }],
            PayementMethod = "CreditCard",
            ShippingAddress = "456 Oak Ave",
            UserId = "user1"
        };

        SetupImpostorResponses(
            inventoryItems: [new ProductInventoryItem(1, 10, 50)],
            products: [new GetProductResponseDto(1, 30.00m, "Gadget")]);

        var result = await _sut.CreateOrder(orderDto, CancellationToken.None);

        result.Should().NotBeNull();
        await _publishEndpoint.Received(1)
            .Publish(Arg.Is<OrderSubmitted>(e =>
                e.PaymentMethod == Events.PaymentMethods.CreditCard &&
                e.PaymentItems != null &&
                e.PaymentItems.Count == 1),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrder_WithNullOrder_ThrowsArgumentNullException()
    {
        var act = () => _sut.CreateOrder(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOrder_WithEmptyProducts_ThrowsArgumentException()
    {
        var orderDto = new OrderDto
        {
            Products = [],
            PayementMethod = "CashOnDelivery",
            ShippingAddress = "Test"
        };

        var act = () => _sut.CreateOrder(orderDto, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one product*");
    }

    [Fact]
    public async Task CreateOrder_WithUnavailableProduct_ThrowsException()
    {
        var orderDto = new OrderDto
        {
            Products = [new OrderItemDto { ProductId = 1, Quantity = 200 }],
            PayementMethod = "CashOnDelivery",
            ShippingAddress = "Test",
            UserId = "user1"
        };

        SetupImpostorResponses(
            inventoryItems: [new ProductInventoryItem(1, 10, 5)],
            products: [new GetProductResponseDto(1, 10m, "Item")]);

        var act = () => _sut.CreateOrder(orderDto, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*unavailable*");
    }

    [Fact]
    public async Task CreateOrder_WithInvalidPaymentMethod_ThrowsArgumentException()
    {
        var orderDto = new OrderDto
        {
            Products = [new OrderItemDto { ProductId = 1, Quantity = 1 }],
            PayementMethod = "Bitcoin",
            ShippingAddress = "Test",
            UserId = "user1"
        };

        SetupImpostorResponses(
            inventoryItems: [new ProductInventoryItem(1, 10, 50)],
            products: [new GetProductResponseDto(1, 10m, "Item")]);

        var act = () => _sut.CreateOrder(orderDto, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid payment method*");
    }

    [Fact]
    public async Task CreateOrder_WithMultipleProducts_CalculatesCorrectTotalPrice()
    {
        var orderDto = new OrderDto
        {
            Products =
            [
                new OrderItemDto { ProductId = 1, Quantity = 2 },
                new OrderItemDto { ProductId = 2, Quantity = 3 }
            ],
            PayementMethod = "CashOnDelivery",
            ShippingAddress = "Test",
            UserId = "user1"
        };

        SetupImpostorResponses(
            inventoryItems:
            [
                new ProductInventoryItem(1, 10, 100),
                new ProductInventoryItem(2, 20, 100)
            ],
            products:
            [
                new GetProductResponseDto(1, 10.00m, "Product A"),
                new GetProductResponseDto(2, 15.00m, "Product B")
            ]);

        var result = await _sut.CreateOrder(orderDto, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalPrice.Should().Be(65.00m); // (2 * 10) + (3 * 15)
        result.OrderItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_WithCashOnDelivery_PublishesEventWithEmptyPaymentItems()
    {
        var orderDto = new OrderDto
        {
            Products = [new OrderItemDto { ProductId = 1, Quantity = 1 }],
            PayementMethod = "CashOnDelivery",
            ShippingAddress = "Test",
            UserId = "user1"
        };

        SetupImpostorResponses(
            inventoryItems: [new ProductInventoryItem(1, 10, 50)],
            products: [new GetProductResponseDto(1, 10m, "Item")]);

        await _sut.CreateOrder(orderDto, CancellationToken.None);

        await _publishEndpoint.Received(1)
            .Publish(Arg.Is<OrderSubmitted>(e =>
                e.PaymentMethod == Events.PaymentMethods.CashOnDelivery &&
                e.PaymentItems != null &&
                e.PaymentItems.Count == 0),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrder_WhenInventoryServiceTimesOut_ThrowsTimeoutException()
    {
        var orderDto = new OrderDto
        {
            Products = [new OrderItemDto { ProductId = 1, Quantity = 1 }],
            PayementMethod = "CashOnDelivery",
            ShippingAddress = "Test",
            UserId = "user1"
        };

        _inventoryClient.GetResponse<ProductInventoryAvailibityForOrderResponse>(
            Arg.Any<ProductInventoryAvailibityForOrderRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Response<ProductInventoryAvailibityForOrderResponse>>(
                new MassTransit.RequestTimeoutException("test-request-id")));

        var act = () => _sut.CreateOrder(orderDto, CancellationToken.None);

        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*Timeout waiting for inventory/product services*");
    }

    #endregion

    #region DeleteOrder

    [Fact]
    public async Task DeleteOrder_WithZeroId_ReturnsFalse()
    {
        var result = await _sut.DeleteOrder(0, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteOrder_WithValidOwner_ReturnsTrue()
    {
        SetupHttpContextUser("user1");

        var order = CreateOrder(userId: "user1", totalPrice: 100m);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteOrder(order.Id, CancellationToken.None);

        result.Should().BeTrue();
        (await _context.Orders.FindAsync(order.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteOrder_WithDifferentUser_ReturnsFalse()
    {
        SetupHttpContextUser("otherUser");

        var order = CreateOrder(userId: "user1", totalPrice: 100m);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteOrder(order.Id, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteOrder_WithNonExistentOrderId_ReturnsFalse()
    {
        SetupHttpContextUser("user1");

        var result = await _sut.DeleteOrder(999, CancellationToken.None);

        result.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private void SetupImpostorResponses(
        List<ProductInventoryItem> inventoryItems,
        List<GetProductResponseDto> products)
    {
        var inventoryResponse = Substitute.For<Response<ProductInventoryAvailibityForOrderResponse>>();
        inventoryResponse.Message.Returns(new ProductInventoryAvailibityForOrderResponse(inventoryItems));

        _inventoryClient.GetResponse<ProductInventoryAvailibityForOrderResponse>(
            Arg.Any<ProductInventoryAvailibityForOrderRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(inventoryResponse);

        var productResponse = Substitute.For<Response<GetProductResponse>>();
        productResponse.Message.Returns(new GetProductResponse(products));

        _productClient.GetResponse<GetProductResponse>(
            Arg.Any<GetProductRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(productResponse);
    }

    private void SetupHttpContextUser(string userId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    private static Order CreateOrder(string userId, decimal totalPrice) => new()
    {
        UserId = userId,
        TotalPrice = totalPrice,
        ShippingAddress = "Test Address",
        PayementMethod = Orders.Data.Enums.PaymentMethods.CashOnDelivery,
        OrderItems = []
    };

    #endregion
}

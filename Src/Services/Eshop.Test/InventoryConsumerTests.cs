using Eshop.Events;
using Eshop.Inventory.Data;
using Eshop.Inventory.Handler;
using Eshop.Inventory.Services;
using FluentAssertions;
using Imposter.Abstractions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Eshop.Test;

public class InventoryConsumerTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private SqliteConnection _connection = null!;


    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var requestClientImposter = IRequestClient<VerifyProductExistence>.Imposter();
        var loggerImposter = ILogger<InventoryService>.Imposter();

        _provider = new ServiceCollection()
            .AddDbContext<InventoryDb>(opts =>
                opts.UseSqlite(_connection))
            .AddScoped<IInventoryService, InventoryService>()
            .AddSingleton(requestClientImposter.Instance())
            .AddSingleton(loggerImposter.Instance())
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<ReductInventoryQuantityFromAnOrderConsumer>();
            })
            .BuildServiceProvider(true);

        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDb>();
            await db.Database.EnsureCreatedAsync();
        }

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Consume_SufficientInventory_ReducesQuantityAndPublishesReserved()
    {
        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDb>();
            db.Inventories.Add(new Eshop.Inventory.Models.Inventory
            {
                Id = 1,
                ProductId = 1,
                Quantity = 100
            });
            await db.SaveChangesAsync();
        }

        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new ReductInventoryQuantityFromAnOrder
        {
            CorrelationId = correlationId,
            Products = [new InventoryUpdateDto { ProductId = 1, Quantity = 10 }]
        });

        var consumerHarness = _harness.GetConsumerHarness<ReductInventoryQuantityFromAnOrderConsumer>();
        (await consumerHarness.Consumed.Any<ReductInventoryQuantityFromAnOrder>()).Should().BeTrue();
        (await _harness.Published.Any<InventoryReserved>()).Should().BeTrue();

        using var scope2 = _provider.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<InventoryDb>();
        var inventory = await db2.Inventories.FindAsync(1);
        inventory!.Quantity.Should().Be(90);
    }

    [Fact]
    public async Task Consume_InsufficientInventory_PublishesOrderFailed()
    {
        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDb>();
            db.Inventories.Add(new Eshop.Inventory.Models.Inventory
            {
                Id = 1,
                ProductId = 1,
                Quantity = 3
            });
            await db.SaveChangesAsync();
        }

        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new ReductInventoryQuantityFromAnOrder
        {
            CorrelationId = correlationId,
            Products = [new InventoryUpdateDto { ProductId = 1, Quantity = 50 }]
        });

        var consumerHarness = _harness.GetConsumerHarness<ReductInventoryQuantityFromAnOrderConsumer>();
        (await consumerHarness.Consumed.Any<ReductInventoryQuantityFromAnOrder>()).Should().BeTrue();
        (await _harness.Published.Any<OrderFailed>()).Should().BeTrue();
    }

    [Fact]
    public async Task Consume_MultipleProducts_ReducesAllQuantities()
    {
        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDb>();
            db.Inventories.AddRange(
                new Eshop.Inventory.Models.Inventory { Id = 1, ProductId = 1, Quantity = 50 },
                new Eshop.Inventory.Models.Inventory { Id = 2, ProductId = 2, Quantity = 30 });
            await db.SaveChangesAsync();
        }

        await _harness.Bus.Publish(new ReductInventoryQuantityFromAnOrder
        {
            CorrelationId = Guid.NewGuid(),
            Products =
            [
                new InventoryUpdateDto { ProductId = 1, Quantity = 5 },
                new InventoryUpdateDto { ProductId = 2, Quantity = 10 }
            ]
        });

        var consumerHarness = _harness.GetConsumerHarness<ReductInventoryQuantityFromAnOrderConsumer>();
        (await consumerHarness.Consumed.Any<ReductInventoryQuantityFromAnOrder>()).Should().BeTrue();
        (await _harness.Published.Any<InventoryReserved>()).Should().BeTrue();

        using var scope2 = _provider.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<InventoryDb>();
        (await db2.Inventories.FindAsync(1))!.Quantity.Should().Be(45);
        (await db2.Inventories.FindAsync(2))!.Quantity.Should().Be(20);
    }
}

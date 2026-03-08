using Eshop.Events;
using Eshop.Inventory.Data;
using Eshop.Inventory.Handler;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit;

namespace Eshop.Test;

public class InventoryConsumerTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddDbContext<InventoryDb>(opts =>
                opts.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<ReductInventoryQuantityFromAnOrderConsumer>();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
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

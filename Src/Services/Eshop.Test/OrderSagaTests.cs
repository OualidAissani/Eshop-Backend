using Eshop.Events;
using Eshop.Orders.Sagas;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Eshop.Test;

public class OrderSagaTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<OrderStateMachineSaga, OrderState> _sagaHarness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<OrderStateMachineSaga, OrderState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();

        _sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachineSaga, OrderState>();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task OrderSubmitted_CashOnDelivery_TransitionsToReservingInventory()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmitted
        {
            CorrelationId = correlationId,
            OrderId = 1,
            Total = 100m,
            Email = "test@test.com",
            PaymentMethod = PaymentMethods.CashOnDelivery,
            Products = [new InventoryUpdateDto { ProductId = 1, Quantity = 2 }]
        });

        (await _sagaHarness.Consumed.Any<OrderSubmitted>()).Should().BeTrue();
        (await _harness.Published.Any<ReductInventoryQuantityFromAnOrder>()).Should().BeTrue();

        var instance = _sagaHarness.Sagas.ContainsInState(
            correlationId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.ReservingInventory);
        instance.Should().NotBeNull();
    }

    [Fact]
    public async Task OrderSubmitted_CreditCard_TransitionsToProcessingPayment()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmitted
        {
            CorrelationId = correlationId,
            OrderId = 2,
            Total = 200m,
            Email = "test@test.com",
            PaymentMethod = PaymentMethods.CreditCard,
            Products = [new InventoryUpdateDto { ProductId = 1, Quantity = 1 }],
            PaymentItems =
            [
                new OrderItemSagaDto
                {
                    name = "Gadget",
                    quantity = 1,
                    description = "Order item",
                    unit_amount = new AmountDto { value = 200m, currency_code = "USD" }
                }
            ]
        });

        (await _sagaHarness.Consumed.Any<OrderSubmitted>()).Should().BeTrue();
        (await _harness.Published.Any<ProcessPayment>()).Should().BeTrue();

        var instance = _sagaHarness.Sagas.ContainsInState(
            correlationId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.ProcessingPayment);
        instance.Should().NotBeNull();
    }

    [Fact]
    public async Task PaymentProcessed_TransitionsToReservingInventory()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmitted
        {
            CorrelationId = correlationId,
            OrderId = 3,
            Total = 150m,
            Email = "test@test.com",
            PaymentMethod = PaymentMethods.PayPal,
            Products = [new InventoryUpdateDto { ProductId = 1, Quantity = 1 }],
            PaymentItems =
            [
                new OrderItemSagaDto
                {
                    name = "Widget",
                    quantity = 1,
                    description = "Test",
                    unit_amount = new AmountDto { value = 150m, currency_code = "USD" }
                }
            ]
        });

        (await _sagaHarness.Consumed.Any<OrderSubmitted>()).Should().BeTrue();

        await _harness.Bus.Publish(new PaymentProcessed
        {
            CorrelationId = correlationId,
            OrderId = 3,
            PaymentIntentId = "pi_test_123"
        });

        (await _sagaHarness.Consumed.Any<PaymentProcessed>()).Should().BeTrue();
        (await _harness.Published.Any<ReductInventoryQuantityFromAnOrder>()).Should().BeTrue();

        var instance = _sagaHarness.Sagas.ContainsInState(
            correlationId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.ReservingInventory);
        instance.Should().NotBeNull();
    }

    [Fact]
    public async Task InventoryReserved_CashOnDelivery_TransitionsToCompleted()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmitted
        {
            CorrelationId = correlationId,
            OrderId = 4,
            Total = 75m,
            Email = "test@test.com",
            PaymentMethod = PaymentMethods.CashOnDelivery,
            Products = [new InventoryUpdateDto { ProductId = 1, Quantity = 3 }]
        });

        (await _sagaHarness.Consumed.Any<OrderSubmitted>()).Should().BeTrue();

        await _harness.Bus.Publish(new InventoryReserved
        {
            CorrelationId = correlationId
        });

        (await _sagaHarness.Consumed.Any<InventoryReserved>()).Should().BeTrue();
        (await _harness.Published.Any<OrderConfirmed>()).Should().BeTrue();

        var instance = _sagaHarness.Sagas.ContainsInState(
            correlationId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Completed);
        instance.Should().NotBeNull();
    }

    [Fact]
    public async Task OrderFailed_DuringPayment_PublishesCompensation()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmitted
        {
            CorrelationId = correlationId,
            OrderId = 5,
            Total = 300m,
            Email = "test@test.com",
            PaymentMethod = PaymentMethods.CreditCard,
            Products = [new InventoryUpdateDto { ProductId = 1, Quantity = 1 }],
            PaymentItems =
            [
                new OrderItemSagaDto
                {
                    name = "Premium Item",
                    quantity = 1,
                    description = "Test",
                    unit_amount = new AmountDto { value = 300m, currency_code = "USD" }
                }
            ]
        });

        (await _sagaHarness.Consumed.Any<OrderSubmitted>()).Should().BeTrue();

        await _harness.Bus.Publish(new OrderFailed
        {
            CorrelationId = correlationId
        });

        (await _sagaHarness.Consumed.Any<OrderFailed>()).Should().BeTrue();
        (await _harness.Published.Any<OrderCompensate>()).Should().BeTrue();
    }

    [Fact]
    public async Task OrderFailed_DuringInventoryReservation_PublishesCompensationAndRefund()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new OrderSubmitted
        {
            CorrelationId = correlationId,
            OrderId = 6,
            Total = 250m,
            Email = "test@test.com",
            PaymentMethod = PaymentMethods.CashOnDelivery,
            Products = [new InventoryUpdateDto { ProductId = 1, Quantity = 5 }]
        });

        (await _sagaHarness.Consumed.Any<OrderSubmitted>()).Should().BeTrue();

        var instance = _sagaHarness.Sagas.ContainsInState(
            correlationId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.ReservingInventory);
        instance.Should().NotBeNull();

        await _harness.Bus.Publish(new OrderFailed
        {
            CorrelationId = correlationId
        });

        (await _sagaHarness.Consumed.Any<OrderFailed>()).Should().BeTrue();
        (await _harness.Published.Any<OrderCompensate>()).Should().BeTrue();
        (await _harness.Published.Any<RefundPayment>()).Should().BeTrue();

        var failedInstance = _sagaHarness.Sagas.ContainsInState(
            correlationId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Failed);
        failedInstance.Should().NotBeNull();
    }
}

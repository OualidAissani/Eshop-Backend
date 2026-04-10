using Eshop.Events;
using Eshop.Orders.Data.Enums;
using MassTransit;
using System.Text.Json;

namespace Eshop.Orders.Sagas
{
    public class OrderState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public int OrderId { get; set; }
        public decimal OrderTotal { get; set; }
        public Data.Enums.PaymentMethods PaymentMethod { get; set; }
        public string? ProductsJson { get; set; }
        public string? PaymentIntentId { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? CustomerEmail { get; set; }

        public List<InventoryUpdateDto> GetProducts() =>
            string.IsNullOrEmpty(ProductsJson)
                ? []
                : JsonSerializer.Deserialize<List<InventoryUpdateDto>>(ProductsJson) ?? [];

        public void SetProducts(List<InventoryUpdateDto> products) =>
            ProductsJson = JsonSerializer.Serialize(products);
    }

    public class OrderStateMachineSaga : MassTransitStateMachine<OrderState>
    {
        private readonly ILogger<OrderStateMachineSaga> _logger;
        public OrderStateMachineSaga(ILogger<OrderStateMachineSaga> logger)
        {

            Event(() => OrderSubmitted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => InventoryReserved, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => PaymentProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderFailed, x => x.CorrelateById(context => context.Message.CorrelationId));

            InstanceState(x => x.CurrentState);

            Initially(
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        _logger.LogInformation($"OrderSubmitted received for OrderId: {context.Message.OrderId}");

                        context.Saga.OrderTotal = context.Message.Total;
                        context.Saga.CustomerEmail = context.Message.Email;
                        context.Saga.OrderDate = DateTime.UtcNow;
                        context.Saga.OrderId = context.Message.OrderId;
                        context.Saga.PaymentMethod = (Data.Enums.PaymentMethods)context.Message.PaymentMethod;
                        context.Saga.SetProducts(context.Message.Products);
                    })
                   .IfElse(
                    context => context.Saga.PaymentMethod == Data.Enums.PaymentMethods.CashOnDelivery,
                    then => then
                     .PublishAsync(context => context.Init<ReductInventoryQuantityFromAnOrder>(new
                     {
                         CorrelationId = context.Message.CorrelationId,
                         OrderId = context.Saga.OrderId,
                         Products = context.Saga.GetProducts()
                     }))
                     .TransitionTo(ReservingInventory),
                 @else => @else                   
                    .TransitionTo(ProcessingPayment)
                     )
            );

            During(ProcessingPayment,
                When(PaymentProcessed)
                .PublishAsync(context => context.Init<ReductInventoryQuantityFromAnOrder>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    OrderId = context.Saga.OrderId,
                    Products = context.Saga.GetProducts()
                }))
                .TransitionTo(ReservingInventory)
                , When(OrderFailed)
                .PublishAsync(context => context.Init<OrderCompensate>(new
                {
                    OrderId = context.Saga.OrderId
                }))
                .TransitionTo(Failed)
                .Finalize()
                );

            During(ReservingInventory,
                When(InventoryReserved)
                .PublishAsync(context => context.Init<OrderConfirmed>(new
                {
                    OrderId = context.Saga.OrderId,
                }))
                .TransitionTo(Completed)
                .Finalize()
                , When(OrderFailed)
                .PublishAsync(context => context.Init<OrderCompensate>(new
                {
                    OrderId = context.Saga.OrderId
                }))
                .PublishAsync(context => context.Init<RefundPayment>(new
                {
                    CorrelationId= context.Saga.CorrelationId,
                    OrderId = context.Saga.OrderId,
                    Amount=context.Saga.OrderTotal
                }))
                .TransitionTo(Failed)
                .Finalize()
                );

            SetCompletedWhenFinalized();
        }
        public State ReservingInventory { get; private set; }
        public State ProcessingPayment { get; private set; }
        public State Completed { get; private set; }
        public State Failed { get; private set; }

        public Event<OrderSubmitted> OrderSubmitted { get; private set; }
        public Event<InventoryReserved> InventoryReserved { get; private set; }
        public Event<OrderFailed> OrderFailed { get; private set; }
        public Event<PaymentProcessed> PaymentProcessed { get; set; }

    }
}

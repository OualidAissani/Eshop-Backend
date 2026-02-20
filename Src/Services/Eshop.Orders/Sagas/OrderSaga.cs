using Eshop.Events;
using MassTransit;

namespace Eshop.Orders.Sagas
{
    public class OrderState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public int OrderId { get; set; }
        public decimal OrderTotal { get; set; }
        public string? PaymentIntentId { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? CustomerEmail { get; set; }
    }

    public class OrderStateMachineSaga: MassTransitStateMachine<OrderState>
    {

        public OrderStateMachineSaga()
        {

            Event(() => OrderSubmitted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => InventoryReserved, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderFailed, x => x.CorrelateById(context => context.Message.CorrelationId));

            InstanceState(x => x.CurrentState);

            Initially(
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        context.Saga.OrderTotal = context.Message.Total;
                        context.Saga.CustomerEmail = context.Message.Email;
                        context.Saga.OrderDate = DateTime.UtcNow;
                        context.Saga.OrderId = context.Message.OrderId;
                    })
                    .PublishAsync(context => context.Init<ReductInventoryQuantityFromAnOrder>(new
                    {
                        CorrelationId=context.Message.CorrelationId,
                        OrderId= context.Saga.OrderId,
                        products = context.Message.Products
                    }
                    ))
                    .TransitionTo(ReservingInventory)
            );
            //should be payment before (later)
            During(ReservingInventory,
                When(InventoryReserved)
                .PublishAsync(context => context.Init<OrderConfirmed>(new
                {
                    CorrelationId= context.Saga.CorrelationId,
                    OrderId = context.Saga.OrderId,
                    
                }))
                .TransitionTo(Completed)
                .Finalize()
                , When(OrderFailed)
                .PublishAsync(context => context.Init<OrderCompensate>(new
                {
                    
                    OrderId = context.Saga.OrderId
                }))
                .TransitionTo(Failed)
                .Finalize()
                );


            SetCompletedWhenFinalized();
        }



        public State ReservingInventory { get; private set; }
        public State Completed { get; private set; }
        public State Failed { get; private set; }


        public Event<OrderSubmitted> OrderSubmitted { get; private set; }
            public Event<InventoryReserved> InventoryReserved { get; private set; }
            public Event<OrderFailed> OrderFailed { get; private set; }


    
}


}

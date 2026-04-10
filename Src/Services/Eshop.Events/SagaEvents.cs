namespace Eshop.Events;

public record OrderSubmitted
{
    public Guid CorrelationId { get; set; }

    public int OrderId { get; init; }
    public decimal Total { get; init; }
    public string Email { get; init; }
    public PaymentMethods PaymentMethod { get; set; }
    public List<OrderItemSagaDto>? PaymentItems { get; set; }
    public List<InventoryUpdateDto> Products { get; set; }


}
public enum PaymentMethods
{
    CashOnDelivery,
    CreditCard,
    PayPal
}
public class InventoryDto
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
}

public record ProcessPayment
{
    public Guid CorrelationId { get; set; }

    public int OrderId { get; init; }
    public List<OrderItemSagaDto> Items { get; set; }
    public decimal Amount { get; init; }
}
public class OrderItemSagaDto
{
    public string name { get; set; }

    public int quantity { get; set; }

    public string description { get; set; }

    public AmountDto unit_amount { get; set; }
}

public record PaymentProcessed
{
    public Guid CorrelationId { get; set; }

    public int OrderId { get; init; }
    public string PaymentIntentId { get; init; }
}

public record ReserveInventory
{
    public Guid CorrelationId { get; set; }
    public string OrderId { get; init; }
    List<InventoryDto> Products { get; init; }
}

public record InventoryReserved
{
    public Guid CorrelationId { get; set; }
}

public record RefundPayment
{
    public Guid CorrelationId { get; set; }
    public int OrderId { get; init; }
    public decimal Amount { get; init; }
}

public record OrderConfirmed
{
    public int OrderId { get; init; }
}

public record OrderFailed
{
    public Guid CorrelationId { get; set; }

    //public string OrderId { get; init; }
    //public string Reason { get; init; }
}
public record OrderCompensate
{
    public int OrderId { get; set; }
}

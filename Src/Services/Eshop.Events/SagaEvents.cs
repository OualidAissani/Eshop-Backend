namespace Eshop.Events;

public record OrderSubmitted
{
    public Guid CorrelationId { get; set; }

    public int OrderId { get; init; }
    public decimal Total { get; init; }
    public string Email { get; init; }

    public List<InventoryDto> Products { get; set; }


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
    public decimal Amount { get; init; }
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
    public Guid CorrelationId { get; set; }

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

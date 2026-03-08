namespace Eshop.Events
{
    public record PaypalCheckout(
    List<ItemsDto> items,
    AmountDto Amount
    );
    public class ItemsDto
    {
        public string name { get; set; }

        public int quantity { get; set; }

        public string description { get; set; }

        public AmountDto unit_amount { get; set; }

    }
    public class AmountDto
    {
        public string currency_code { get; set; } = "USD";
        public decimal value { get; set; }
    }

    public record CreatePaymentRecordRequest
    {
        public List<OrderItemSagaDto> Items { get; set; }
        public decimal Amount { get; init; }
        public Guid CorrelationId { get; set; }
        public int OrderId { get; set; }
        public CreatePaymentRecordRequest()
        {

        }
        public CreatePaymentRecordRequest(List<OrderItemSagaDto> items, decimal amount)
        {
            Items = items;
            Amount = amount;
        }
    };
    public record CreatePaymentRecordResponse
    {

        public string PaymentUrl { get; init; }

        public CreatePaymentRecordResponse(string paymentUrl)
        {
            PaymentUrl = paymentUrl;
        }

        public CreatePaymentRecordResponse()
        {

        }
    }
}

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

namespace Eshop.Payement.Models
{
    public class CreateOrder
    {
        public List<ItemsDto> Items { get; set; } = new List<ItemsDto>();
        public AmountDto Amount { get; set; }
    }
}

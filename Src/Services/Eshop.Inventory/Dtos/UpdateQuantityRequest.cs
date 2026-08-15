namespace Eshop.Inventory.Dtos
{
    public class UpdateQuantityRequest
    {
        public List<InventoryDto> Items { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}

using Eshop.Catalog.Data.Enums;

namespace Eshop.Catalog.Dtos
{
    public class DiscountReturnDto
    {
        public string Id { get; set; }
        public DiscountType Type { get; set; }
        public decimal Value { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int TimesUsed { get; set; }
        public bool IsActive { get; set; }
        public int ProductId { get; set; }
    }
}

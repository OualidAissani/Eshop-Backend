using Eshop.Orders.Data.Enums;

namespace Eshop.Orders.Dtos
{
    public class DeliveryPriceDto
    {
        public int WilayaCode { get; set; }

        public DeliveryType DeliveryType { get; set; }

        public decimal PriceDzd { get; set; }
    }
}

using Eshop.Orders.Data.Enums;

namespace Eshop.Orders.Entities;

public class DeliveryPrice
{
    public int Id { get; set; }

    public int WilayaCode { get; set; }

    public DeliveryType DeliveryType { get; set; }

    public decimal PriceDzd { get; set; }
}

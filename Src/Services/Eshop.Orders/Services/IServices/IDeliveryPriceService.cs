using Eshop.Orders.Data.Enums;
using Eshop.Orders.Dtos;

namespace Eshop.Orders.Services.IServices
{
    public interface IDeliveryPriceService
    {
        /// <summary>
        /// Returns the configured price for a destination wilaya and delivery type,
        /// or null when that route is not in the chart.
        /// </summary>
        Task<decimal?> GetPriceAsync(int wilayaCode, DeliveryType deliveryType, CancellationToken ct);

        Task<List<DeliveryPriceDto>> GetAllAsync(CancellationToken ct);

        /// <summary>
        /// Inserts or updates the given rows. Used to load the delivery company's chart.
        /// </summary>
        Task UpsertAsync(IEnumerable<DeliveryPriceDto> prices, CancellationToken ct);
    }
}

using Eshop.Catalog.Dtos;
using Eshop.Catalog.Entities;
using FluentResults;

namespace Eshop.Catalog.Services.IServices
{
    public interface IDiscountService
    {
        Task<Result<DiscountDocument>> AddDiscount(DiscountDto discount, CancellationToken ct);
        Task<Result<DiscountDocument>> UpdateDiscount(DiscountDto discount,CancellationToken ct);
        Task<Result<DiscountDocument>> GetDiscountByProductId(int id,CancellationToken ct);
        Task<Result<List<DiscountDocument>>> GetDiscounts(CancellationToken ct);
        Task<Result<bool>> DeleteDiscount(int productId, CancellationToken ct);

    }
}

using Eshop.Orders.Models;
using Refit;

namespace Eshop.Orders.Services.IServices
{
    public interface IUpdateInventory
    {
        [Put("/UpdatePrice")]
        Task<ApiResponse<object>> UpdateInventory([Body] List<InventoryUpdateDto> UpdateValues);
       
    }

}

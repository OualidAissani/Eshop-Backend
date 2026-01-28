using Refit;

namespace Eshop.Orders.Services.IServices
{
    public interface IUpdateInventory
    {
        [Post("/UpdatePrice")]
        Task<ApiResponse<object>> UpdateInventory(List<InventoryUpdateDto> UpdateValues);
       
    }

    public class InventoryUpdateDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

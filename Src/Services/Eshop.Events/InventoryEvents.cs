
namespace Eshop.Events;

public record ProductInventoryAvailibityForOrderRequest
{
    public int Product { get; set; }
    public int Quantity { get; set; }
    public ProductInventoryAvailibityForOrderRequest()
    {
        
    }
    public ProductInventoryAvailibityForOrderRequest(int product,int quantity)
    {
        Product = product;
        Quantity = quantity;
    }
}
public record ProductInventoryAvailibityForOrderResponse
{
    //bool IsAvailable,int inventoryId
    public bool IsAvailable { get; set; }
    public int inventoryId { get; set; }
    public ProductInventoryAvailibityForOrderResponse()
    {
        
    }
    public ProductInventoryAvailibityForOrderResponse(bool isAvailable,int InventoryId)
    {
        IsAvailable = isAvailable;
        inventoryId = InventoryId;
    }
}



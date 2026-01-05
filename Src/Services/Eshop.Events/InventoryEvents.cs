
namespace Eshop.Events;

public record ProductInventoryAvailibityForOrderRequest(int Product,int Quantity);
public record ProductInventoryAvailibityForOrderResponse(bool IsAvailable,int inventoryId);



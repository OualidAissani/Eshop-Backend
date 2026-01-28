namespace Eshop.Events;

public record ProductInventoryAvailibityForOrderRequest
{
    public List<int> ProductsId { get; set; }

    public ProductInventoryAvailibityForOrderRequest()
    {
    }

    public ProductInventoryAvailibityForOrderRequest(List<int> product)
    {
        ProductsId = product;
    }
}

public record ProductInventoryItem
{
    public int ProductId { get; set; }
    public int InventoryId { get; set; }
    public int Quantity { get; set; }

    public ProductInventoryItem()
    {
    }

    public ProductInventoryItem(int productId, int inventoryId, int quantity)
    {
        ProductId = productId;
        InventoryId = inventoryId;
        Quantity = quantity;
    }
}

public record ProductInventoryAvailibityForOrderResponse
{
    public IEnumerable<ProductInventoryItem> Items { get; set; }

    public ProductInventoryAvailibityForOrderResponse()
    {
    }

    public ProductInventoryAvailibityForOrderResponse(IEnumerable<ProductInventoryItem> items)
    {
        Items = items;
    }
}



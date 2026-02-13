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





public record ProductStockRequest
{
    public int ProductsId { get; set; }
    public int  Quantity { get; set; }

    public ProductStockRequest()
    {
    }

    public ProductStockRequest(int product, int quantity)
    {
        ProductsId = product;
        Quantity = quantity;
    }
}


public record ProductStockResponse
{
    public bool HasEnoguhStock { get; set; }

    public ProductStockResponse()
    {
    }

    public ProductStockResponse(bool HasEnoguhStocks)
    {
        HasEnoguhStock = HasEnoguhStocks;
    }
}



public record ReductInventoryQuantityFromAnOrder(
    List<InventoryUpdateDto> Products
);





public class InventoryUpdateDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}



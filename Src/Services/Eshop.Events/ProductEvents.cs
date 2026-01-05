
namespace Eshop.Events;

    public record OrderedProduct(
        Dictionary<string, int> Products
        );
    public record CheckProductAvailibility(
        int ProductId,int Quantity
        );
    public record RetrieveProductPrice(
        List<int> ProductId
        );
public record GetProductRequest(int ProductId);
public record GetProductResponse(int ProductId, int UnitPrice);



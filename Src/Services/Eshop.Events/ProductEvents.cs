
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
public record GetProductRequest
{
    public int ProductId { get; init; }

    public GetProductRequest() { }
    public GetProductRequest(int productId)
    {
        ProductId = productId;
    }

}
public record GetProductResponse
{
    public int ProductId { get; init; }
    public double UnitPrice { get; init; }

    public GetProductResponse() { }
    public GetProductResponse(int productId, double unitPrice)
    {
        ProductId = productId;
        UnitPrice = unitPrice;
    }
}



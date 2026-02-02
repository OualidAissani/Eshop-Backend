
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
    public List<int> ProductId { get; init; }

    public GetProductRequest() { }
    public GetProductRequest(List<int> productId)
    {
        ProductId = productId;
    }

}
public class GetProductResponseDto
{
    public int Id { get; set; }
    public decimal Price { get; set; }
    public GetProductResponseDto()
    {
        
    }
    public GetProductResponseDto(int id,decimal price)
    {
        Id=id;
        Price=price;
    }
}
public record GetProductResponse
{
    public List<GetProductResponseDto> Product { get; init; }

    public GetProductResponse() { }
    public GetProductResponse(List<GetProductResponseDto> product)
    {
        Product = product;
    }
}

public record ProductExistRequest
{
    public int ProductId { get; init; }
    public ProductExistRequest()
    {
        
    }
    public ProductExistRequest(int productId)
    {
        ProductId=productId;
    }
}

public record ProductExistResponse
{
    public bool IsAvailable { get; init; }
    public ProductExistResponse() { }
    public ProductExistResponse(bool IsAvailable)
    {
        this.IsAvailable = IsAvailable;
    }
}


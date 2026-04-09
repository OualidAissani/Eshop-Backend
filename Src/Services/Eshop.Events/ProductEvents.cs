
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
    public string Name { get; set; }
    public GetProductResponseDto()
    {
        
    }
    public GetProductResponseDto(int id,decimal price,string name)
    {
        Id=id;
        Price=price;
        Name=name;
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


public record UpdateCartProduct(int ProductId , string ProductName , decimal FullPrice);
public record DeleteCartProduct(int ProductId);

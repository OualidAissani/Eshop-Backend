
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
    public double Price { get; set; }
    public GetProductResponseDto()
    {
        
    }
    public GetProductResponseDto(int id,double price)
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

public record RefreshCartItemDetailsRequest
{
    public List<int> ProductId { get; init; }
    public RefreshCartItemDetailsRequest()
    {
        
    }
    public RefreshCartItemDetailsRequest(List<int> productId)
    {
        ProductId=productId;
    }
}
public record RefreshCartItemDetailsResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
    public RefreshCartItemDetailsResponseDto()
    {
        
    }
    public RefreshCartItemDetailsResponseDto(int id,string name,double price)
    {
        Id=id;
        Name=name;
        Price=price;
    }
}
public record RefreshCartItemDetailsResponse
{
    public List<RefreshCartItemDetailsResponseDto> Items { get; init; }
    public RefreshCartItemDetailsResponse() { }
    public RefreshCartItemDetailsResponse(List<RefreshCartItemDetailsResponseDto> items)
    {
        Items = items;
    }
}


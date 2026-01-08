
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



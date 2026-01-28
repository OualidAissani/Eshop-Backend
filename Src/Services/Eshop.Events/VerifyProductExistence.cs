
namespace Eshop.Events;

     public record VerifyProductExistence
    (
         int ProductId
    );
    public record ProductExistenceResponse
    (
         bool Exists
    );




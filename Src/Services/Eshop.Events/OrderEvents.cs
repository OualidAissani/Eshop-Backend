namespace Eshop.Events
{
   public record OrderProducts(
       List<int> products,string UserId,int Price);


}

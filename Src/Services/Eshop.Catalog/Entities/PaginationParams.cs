namespace Eshop.Catalog.Entities
{
    public class PaginationParams
    {
        public int PageSize { get; set; } = 10;
        public int? LastId { get; set; }

        public void Validate()
        {
            if (PageSize < 1) PageSize = 10;
            if (PageSize > 20) PageSize = 20; 
        }
    }
}

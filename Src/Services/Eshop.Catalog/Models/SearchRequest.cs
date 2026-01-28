namespace Eshop.Catalog.Models
{
    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public List<int>? CategoryIds { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsFeatured { get; set; }
        public bool? IsNewArrival { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } // "price:asc", "price:desc", "title:asc"
        
        public void Validate()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1) PageSize = 20;
            if (PageSize > 100) PageSize = 100;
        }
    }
}

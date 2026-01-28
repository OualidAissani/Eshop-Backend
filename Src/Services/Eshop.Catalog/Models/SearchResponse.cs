namespace Eshop.Catalog.Models
{
    public class SearchResponse<T>
    {
        public List<T> Results { get; set; } = new();
        public int TotalHits { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (TotalHits + PageSize - 1) / PageSize;
        public long ProcessingTimeMs { get; set; }
        public string Query { get; set; } = string.Empty;
        
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}

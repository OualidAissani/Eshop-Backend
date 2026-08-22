namespace Eshop.Catalog.Entities
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }

        public int? NextCursor { get; set; }

        public bool HasMore => NextCursor.HasValue;
    }
}

namespace RamaExpress.Areas.Admin.Models
{
    public class KaryawanListViewModel
    {
        public IEnumerable<User> Users { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }

        // Sorting properties
        public string SortField { get; set; } = "Nama";
        public string SortDirection { get; set; } = "asc";

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartRecord => ((CurrentPage - 1) * PageSize) + 1;
        public int EndRecord => Math.Min(CurrentPage * PageSize, TotalCount);

        // Helper methods for sorting UI
        public string GetSortClass(string field)
        {
            if (SortField != field)
                return "bi-chevron-expand text-muted";

            return SortDirection == "asc"
                ? "bi-chevron-up text-warning"
                : "bi-chevron-down text-warning";
        }

        public string GetNextSortDirection(string field)
        {
            if (SortField != field)
                return "asc";

            return SortDirection == "asc" ? "desc" : "asc";
        }
    }
}
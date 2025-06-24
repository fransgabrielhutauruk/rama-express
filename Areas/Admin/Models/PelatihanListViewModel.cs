// Areas/Admin/Models/PelatihanListViewModel.cs
namespace RamaExpress.Areas.Admin.Models
{
    public class PelatihanListViewModel
    {
        public IEnumerable<Pelatihan> Pelatihans { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartRecord => ((CurrentPage - 1) * PageSize) + 1;
        public int EndRecord => Math.Min(CurrentPage * PageSize, TotalCount);
    }
}
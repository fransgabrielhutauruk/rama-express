// Areas/Admin/Models/LaporanPelatihanListViewModel.cs
namespace RamaExpress.Areas.Admin.Models
{
    public class LaporanPelatihanListViewModel
    {
        public IEnumerable<LaporanPelatihanViewModel> Results { get; set; } = new List<LaporanPelatihanViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        // Filter properties
        public string? SearchTerm { get; set; }
        public string? PelatihanFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? PosisiFilter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Filter data
        public IEnumerable<Pelatihan> AvailablePelatihans { get; set; } = new List<Pelatihan>();
        public IEnumerable<string> AvailablePosisis { get; set; } = new List<string>();

        // Statistics
        public int TotalKaryawan { get; set; }
        public int TotalPelatihanAktif { get; set; }
        public int TotalSertifikatTerbit { get; set; }
        public double CompletionRate { get; set; }

        // Pagination helpers
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartRecord => ((CurrentPage - 1) * PageSize) + 1;
        public int EndRecord => Math.Min(CurrentPage * PageSize, TotalCount);

        // Statistics for current results
        public int TotalSelesai => Results.Count(r => r.IsSelesai);
        public int TotalLulus => Results.Count(r => r.IsLulus);
        public int TotalBelumSelesai => Results.Count(r => !r.IsSelesai);
        public double PersentaseKelulusan => TotalSelesai > 0 ? Math.Round((double)TotalLulus / TotalSelesai * 100, 1) : 0;
    }
}
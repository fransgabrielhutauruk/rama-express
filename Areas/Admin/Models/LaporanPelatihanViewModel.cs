// Areas/Admin/Models/LaporanPelatihanViewModel.cs
namespace RamaExpress.Areas.Admin.Models
{
    public class LaporanPelatihanViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string NamaKaryawan { get; set; }
        public string Email { get; set; }
        public string Posisi { get; set; }
        public int PelatihanId { get; set; }
        public string KodePelatihan { get; set; }
        public string JudulPelatihan { get; set; }
        public int DurasiMenit { get; set; }
        public int SkorMinimal { get; set; }
        public DateTime TanggalMulai { get; set; }
        public DateTime? TanggalSelesai { get; set; }
        public bool IsSelesai { get; set; }
        public int? Skor { get; set; }
        public bool IsLulus { get; set; }

        // Computed properties
        public string StatusText => IsSelesai ? "Selesai" : "Belum Selesai";
        public string HasilText => IsLulus ? "Lulus" : "Tidak Lulus";
        public TimeSpan? DurasiPelatihan => TanggalSelesai?.Subtract(TanggalMulai);

        public string GetStatusBadgeClass()
        {
            if (!IsSelesai) return "bg-warning";
            return IsLulus ? "bg-success" : "bg-danger";
        }

        public string GetStatusIcon()
        {
            if (!IsSelesai) return "bi-clock";
            return IsLulus ? "bi-check-circle" : "bi-x-circle";
        }
    }
}

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

// Areas/Admin/Models/LaporanDetailViewModel.cs
namespace RamaExpress.Areas.Admin.Models
{
    public class LaporanDetailViewModel
    {
        public PelatihanProgress Progress { get; set; }
        public PelatihanHasil? Hasil { get; set; }
        public Sertifikat? Sertifikat { get; set; }
        public IEnumerable<PelatihanMateri> Materials { get; set; } = new List<PelatihanMateri>();
        public IEnumerable<PelatihanSoal> Questions { get; set; } = new List<PelatihanSoal>();

        // Computed properties
        public TimeSpan? DurasiPelatihan => Progress.CompletedAt?.Subtract(Progress.StartedAt);
        public int MaterialProgress => Materials.Count() > 0 ? Progress.MateriTerakhirId : 0;
        public double PersentaseProgress => Materials.Count() > 0
            ? Math.Round((double)MaterialProgress / Materials.Count() * 100, 1)
            : 0;
    }
}
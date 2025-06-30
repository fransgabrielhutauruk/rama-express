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
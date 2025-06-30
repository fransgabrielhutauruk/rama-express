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
        public string HasilText => IsLulus ? "Lulus" : (Skor.HasValue ? "Tidak Lulus" : "Belum Ujian");
        public TimeSpan? DurasiPelatihan => TanggalSelesai?.Subtract(TanggalMulai);

        public string GetStatusBadgeClass()
        {
            return IsSelesai ? "bg-success" : "bg-warning";
        }

        public string GetStatusIcon()
        {
            return IsSelesai? "bi-check-circle" : "bi-x-circle";
        }
    }
}
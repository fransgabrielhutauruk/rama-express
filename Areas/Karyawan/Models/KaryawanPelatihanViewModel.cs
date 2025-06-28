// Areas/Karyawan/Models/KaryawanPelatihanViewModel.cs
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Karyawan.Models
{
    public class KaryawanPelatihanViewModel
    {
        public IEnumerable<Pelatihan> AvailableTrainings { get; set; } = new List<Pelatihan>();
        public IEnumerable<PelatihanProgress> OngoingTrainings { get; set; } = new List<PelatihanProgress>();
        public IEnumerable<PelatihanHasil> CompletedTrainings { get; set; } = new List<PelatihanHasil>();

        public int TotalAvailable { get; set; }
        public int TotalOngoing { get; set; }
        public int TotalCompleted { get; set; }

        // Statistics
        public double CompletionPercentage => TotalAvailable > 0 ?
            Math.Round((double)TotalCompleted / TotalAvailable * 100, 1) : 0;

        public double AverageScore => CompletedTrainings.Any() ?
            Math.Round(CompletedTrainings.Average(c => c.Skor), 1) : 0;
    }

    public class PelatihanDetailViewModel
    {
        public Pelatihan Pelatihan { get; set; }
        public PelatihanProgress? Progress { get; set; }
        public PelatihanHasil? Hasil { get; set; }
        public Sertifikat? Sertifikat { get; set; }

        public bool CanStart { get; set; }
        public bool CanContinue { get; set; }
        public bool CanTakeExam { get; set; }
        public bool IsCompleted { get; set; }

        // Progress calculation
        public int MaterialProgress => Progress?.MateriTerakhirId ?? 0;
        public int TotalMaterials => Pelatihan?.PelatihanMateris?.Count() ?? 0;
        public double ProgressPercentage => TotalMaterials > 0 ?
            Math.Round((double)MaterialProgress / TotalMaterials * 100, 1) : 0;

        // Status text
        public string StatusText => GetStatusText();
        public string StatusClass => GetStatusClass();

        private string GetStatusText()
        {
            if (Hasil != null)
                return Hasil.IsLulus ? "Lulus" : "Tidak Lulus";
            if (Progress?.IsCompleted == true)
                return "Siap Ujian";
            if (Progress != null)
                return "Sedang Berjalan";
            return "Belum Dimulai";
        }

        private string GetStatusClass()
        {
            if (Hasil != null)
                return Hasil.IsLulus ? "success" : "danger";
            if (Progress?.IsCompleted == true)
                return "warning";
            if (Progress != null)
                return "info";
            return "secondary";
        }
    }

    public class MateriViewModel
    {
        public Pelatihan Pelatihan { get; set; }
        public PelatihanMateri CurrentMateri { get; set; }
        public PelatihanProgress Progress { get; set; }
        public List<PelatihanMateri> AllMaterials { get; set; } = new();
        public PelatihanMateri? NextMaterial { get; set; }
        public PelatihanMateri? PreviousMaterial { get; set; }
        public bool IsLastMaterial { get; set; }

        // Navigation
        public bool HasNext => NextMaterial != null;
        public bool HasPrevious => PreviousMaterial != null;

        // Progress
        public double ProgressPercentage => AllMaterials.Count > 0 ?
            Math.Round((double)CurrentMateri.Urutan / AllMaterials.Count * 100, 1) : 0;
    }

    public class UjianViewModel
    {
        public Pelatihan Pelatihan { get; set; }
        public List<PelatihanSoal> Questions { get; set; } = new();
        public int TimeLimit { get; set; } // in seconds
        public int MinScore { get; set; }

        public int TotalQuestions => Questions.Count;
        public string TimeLimitFormatted => $"{TimeLimit / 60} menit";
    }

    public class HasilUjianViewModel
    {
        public PelatihanHasil Hasil { get; set; }
        public Sertifikat? Sertifikat { get; set; }

        public bool HasCertificate => Sertifikat != null;
        public string ResultText => Hasil.IsLulus ? "LULUS" : "TIDAK LULUS";
        public string ResultClass => Hasil.IsLulus ? "success" : "danger";
        public string ResultIcon => Hasil.IsLulus ? "bi-check-circle-fill" : "bi-x-circle-fill";
    }
}
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Karyawan.Models
{
    public class SertifikatIndexViewModel
    {
        public IEnumerable<Sertifikat> Certificates { get; set; } = new List<Sertifikat>();
        public int TotalCertificates { get; set; }
        public int ValidCertificates { get; set; }
        public int ExpiredCertificates { get; set; }

        // Statistics
        public double ValidPercentage => TotalCertificates > 0 ?
            Math.Round((double)ValidCertificates / TotalCertificates * 100, 1) : 0;

        // Recent certificates (last 3 months)
        public IEnumerable<Sertifikat> RecentCertificates => Certificates
            .Where(c => c.TanggalTerbit >= DateTime.Now.AddMonths(-3))
            .OrderByDescending(c => c.TanggalTerbit);
    }

    public class SertifikatDetailViewModel
    {
        public Sertifikat Certificate { get; set; } = null!;
        public PelatihanHasil? TrainingResult { get; set; }
        public bool IsValid { get; set; }
        public int? DaysUntilExpiry { get; set; }

        // Status properties
        public string StatusText => GetStatusText();
        public string StatusClass => GetStatusClass();
        public string StatusIcon => GetStatusIcon();

        private string GetStatusText()
        {
            if (!IsValid)
                return "Tidak Berlaku";
            if (DaysUntilExpiry.HasValue && DaysUntilExpiry.Value <= 30)
                return "Akan Berakhir";
            return "Berlaku";
        }

        private string GetStatusClass()
        {
            if (!IsValid)
                return "danger";
            if (DaysUntilExpiry.HasValue && DaysUntilExpiry.Value <= 30)
                return "warning";
            return "success";
        }

        private string GetStatusIcon()
        {
            if (!IsValid)
                return "bi-x-circle-fill";
            if (DaysUntilExpiry.HasValue && DaysUntilExpiry.Value <= 30)
                return "bi-exclamation-triangle-fill";
            return "bi-check-circle-fill";
        }
    }

    public class SertifikatVerificationViewModel
    {
        public string NomorSertifikat { get; set; } = "";
        public Sertifikat? Certificate { get; set; }
        public bool IsValid { get; set; }

        public string VerificationStatus => Certificate == null ? "Tidak Ditemukan" :
                                          IsValid ? "Valid" : "Tidak Berlaku";

        public string VerificationClass => Certificate == null ? "danger" :
                                         IsValid ? "success" : "warning";
    }

    public class SertifikatShareViewModel
    {
        public string ShareUrl { get; set; } = "";
        public string CertificateNumber { get; set; } = "";
        public string TrainingTitle { get; set; } = "";
        public string HolderName { get; set; } = "";
    }
}
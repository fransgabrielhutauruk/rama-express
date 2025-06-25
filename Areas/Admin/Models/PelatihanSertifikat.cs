// Areas/Admin/Models/PelatihanSertifikat.cs
using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Areas.Admin.Models
{
    public class PelatihanSertifikat
    {
        public int Id { get; set; }

        [Required]
        public int PelatihanId { get; set; }

        [Required(ErrorMessage = "Status sertifikat wajib dipilih")]
        [Display(Name = "Status Sertifikat")]
        public bool IsSertifikatActive { get; set; } = true;

        [Required(ErrorMessage = "Nama template sertifikat wajib diisi")]
        [StringLength(200, ErrorMessage = "Nama template maksimal 200 karakter")]
        [Display(Name = "Nama Template Sertifikat")]
        public string TemplateName { get; set; }

        [StringLength(500, ErrorMessage = "Deskripsi template maksimal 500 karakter")]
        [Display(Name = "Deskripsi Template")]
        public string? TemplateDescription { get; set; }

        [Required(ErrorMessage = "Tipe kadaluarsa wajib dipilih")]
        [Display(Name = "Tipe Kadaluarsa")]
        public string ExpirationType { get; set; } // "never", "months", "years"

        [Range(1, 999, ErrorMessage = "Durasi harus antara 1-999")]
        [Display(Name = "Durasi Kadaluarsa")]
        public int? ExpirationDuration { get; set; }

        [StringLength(50, ErrorMessage = "Unit kadaluarsa maksimal 50 karakter")]
        [Display(Name = "Unit Kadaluarsa")]
        public string? ExpirationUnit { get; set; } // "months", "years"

        [Display(Name = "Format Nomor Sertifikat")]
        [StringLength(100, ErrorMessage = "Format nomor maksimal 100 karakter")]
        public string CertificateNumberFormat { get; set; } = "CERT-{YEAR}-{MONTH}-{INCREMENT}";

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Tanggal Diupdate")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual Pelatihan? Pelatihan { get; set; }

        // Helper methods
        public DateTime? CalculateExpirationDate(DateTime issueDate)
        {
            if (ExpirationType == "never")
                return null;

            if (ExpirationType == "months" && ExpirationDuration.HasValue)
                return issueDate.AddMonths(ExpirationDuration.Value);

            if (ExpirationType == "years" && ExpirationDuration.HasValue)
                return issueDate.AddYears(ExpirationDuration.Value);

            return null;
        }

        public string GetExpirationDisplayText()
        {
            if (ExpirationType == "never")
                return "Tidak Ada Kadaluarsa";

            if (ExpirationDuration.HasValue)
            {
                var unit = ExpirationType == "months" ? "bulan" : "tahun";
                return $"{ExpirationDuration} {unit}";
            }

            return "Tidak Ditentukan";
        }
    }
}
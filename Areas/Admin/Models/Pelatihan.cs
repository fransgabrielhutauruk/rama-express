using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Areas.Admin.Models
{
    public class Pelatihan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kode pelatihan wajib diisi")]
        [StringLength(20, ErrorMessage = "Kode maksimal 20 karakter")]
        [Display(Name = "Kode Pelatihan")]
        public string Kode { get; set; }

        [Required(ErrorMessage = "Judul pelatihan wajib diisi")]
        [StringLength(200, ErrorMessage = "Judul maksimal 200 karakter")]
        [Display(Name = "Judul Pelatihan")]
        public string Judul { get; set; }

        [StringLength(500, ErrorMessage = "Deskripsi maksimal 500 karakter")]
        [Display(Name = "Deskripsi")]
        public string? Deskripsi { get; set; }

        [Required(ErrorMessage = "Durasi wajib diisi")]
        [Range(1, 9999, ErrorMessage = "Durasi harus antara 1-9999 menit")]
        [Display(Name = "Durasi (Menit)")]
        public int DurasiMenit { get; set; }

        [Required(ErrorMessage = "Skor minimal wajib diisi")]
        [Range(0, 100, ErrorMessage = "Skor minimal harus antara 0-100")]
        [Display(Name = "Skor Minimal Lulus (%)")]
        public int SkorMinimal { get; set; } = 70;

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Tanggal Diupdate")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Status Dihapus")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Tanggal Dihapus")]
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ICollection<PelatihanPosisi>? PelatihanPosisis { get; set; }
        public virtual ICollection<PelatihanMateri>? PelatihanMateris { get; set; }
        public virtual ICollection<PelatihanSoal>? PelatihanSoals { get; set; }
        public virtual ICollection<PelatihanProgress>? PelatihanProgresses { get; set; }
        public virtual ICollection<PelatihanHasil>? PelatihanHasils { get; set; }
        public virtual ICollection<Sertifikat>? Sertifikats { get; set; }
    }
}
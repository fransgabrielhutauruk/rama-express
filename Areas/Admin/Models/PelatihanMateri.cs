using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Areas.Admin.Models
{
    public class PelatihanMateri
    {
        public int Id { get; set; }

        [Required]
        public int PelatihanId { get; set; }

        [Required(ErrorMessage = "Judul materi wajib diisi")]
        [StringLength(200, ErrorMessage = "Judul maksimal 200 karakter")]
        [Display(Name = "Judul Materi")]
        public string Judul { get; set; }

        [Required(ErrorMessage = "Tipe konten wajib dipilih")]
        [Display(Name = "Tipe Konten")]
        public string TipeKonten { get; set; } // text, video, image

        [Required(ErrorMessage = "Konten wajib diisi")]
        [Display(Name = "Konten")]
        public string Konten { get; set; } // HTML content, video URL, or image path

        [Display(Name = "Urutan")]
        public int Urutan { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Tanggal Diupdate")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual Pelatihan? Pelatihan { get; set; }
    }
}
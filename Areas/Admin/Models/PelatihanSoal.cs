// Areas/Admin/Models/PelatihanSoal.cs
using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Areas.Admin.Models
{
    public class PelatihanSoal
    {
        public int Id { get; set; }

        [Required]
        public int PelatihanId { get; set; }

        [Required(ErrorMessage = "Pertanyaan wajib diisi")]
        [Display(Name = "Pertanyaan")]
        public string Pertanyaan { get; set; }

        [Required(ErrorMessage = "Opsi A wajib diisi")]
        [Display(Name = "Opsi A")]
        public string OpsiA { get; set; }

        [Required(ErrorMessage = "Opsi B wajib diisi")]
        [Display(Name = "Opsi B")]
        public string OpsiB { get; set; }

        [Required(ErrorMessage = "Opsi C wajib diisi")]
        [Display(Name = "Opsi C")]
        public string OpsiC { get; set; }

        [Required(ErrorMessage = "Opsi D wajib diisi")]
        [Display(Name = "Opsi D")]
        public string OpsiD { get; set; }

        [Required(ErrorMessage = "Jawaban benar wajib dipilih")]
        [Display(Name = "Jawaban Benar")]
        public string JawabanBenar { get; set; } // A, B, C, or D

        [Display(Name = "Urutan")]
        public int Urutan { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual Pelatihan? Pelatihan { get; set; }
    }
}
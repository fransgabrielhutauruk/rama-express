using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Areas.Admin.Models
{
    public class Posisi
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama posisi wajib diisi")]
        [StringLength(50, ErrorMessage = "Nama posisi maksimal 50 karakter")]
        [Display(Name = "Nama Posisi")]
        public string Name { get; set; }

        [Display(Name = "Status Dihapus")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Tanggal Dihapus")]
        public DateTime? DeletedAt { get; set; }
    }
}
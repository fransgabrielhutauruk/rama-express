using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Areas.Admin.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string Nama { get; set; } = string.Empty;

        [Required(ErrorMessage = "Posisi wajib diisi")]
        [StringLength(50, ErrorMessage = "Posisi maksimal 50 karakter")]
        [Display(Name = "Posisi/Jabatan")]
        public string Posisi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        public string Email { get; set; } = string.Empty;

        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [StringLength(20, ErrorMessage = "Role maksimal 20 karakter")]
        public string Role { get; set; } = "karyawan";

        [Display(Name = "Tanggal Bergabung")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Status Dihapus")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Terakhir Diupdate")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Tanggal Dihapus")]
        public DateTime? DeletedAt { get; set; }
    }
}
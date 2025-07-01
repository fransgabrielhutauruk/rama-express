// Areas/Karyawan/Models/KaryawanSettingsViewModel.cs
using System.ComponentModel.DataAnnotations;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Karyawan.Models
{
    public class KaryawanSettingsViewModel
    {
        public KaryawanProfileViewModel Profile { get; set; } = new KaryawanProfileViewModel();
        public ChangePasswordViewModel ChangePassword { get; set; } = new ChangePasswordViewModel();
    }

    public class KaryawanProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama wajib diisi")]
        [Display(Name = "Nama Lengkap")]
        [StringLength(100, ErrorMessage = "Nama tidak boleh lebih dari 100 karakter")]
        public string Nama { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100, ErrorMessage = "Email tidak boleh lebih dari 100 karakter")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Posisi")]
        public string? Posisi { get; set; }

        [Display(Name = "Role")]
        public string Role { get; set; } = "Karyawan";

        [Display(Name = "Tanggal Bergabung")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Terakhir Diperbarui")]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; }
    }

    // Service result model
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}
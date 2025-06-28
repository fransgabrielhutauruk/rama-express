// Areas/Admin/Models/SettingsViewModel.cs - FIXED
using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Areas.Admin.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Password lama wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Lama")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password baru minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("NewPassword", ErrorMessage = "Konfirmasi password tidak sesuai dengan password baru")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class AdminProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string Nama { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class SettingsViewModel
    {
        public AdminProfileViewModel Profile { get; set; } = new AdminProfileViewModel();
        public ChangePasswordViewModel ChangePassword { get; set; } = new ChangePasswordViewModel();
    }
}
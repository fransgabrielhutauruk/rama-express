// Areas/Karyawan/Controllers/SettingsController.cs
using Microsoft.AspNetCore.Mvc;
using RamaExpress.Areas.Karyawan.Data.Service;
using RamaExpress.Areas.Karyawan.Models;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Karyawan.Controllers
{
    [Area("Karyawan")]
    public class SettingsController : Controller
    {
        private readonly IKaryawanSettingsService _settingsService;

        public SettingsController(IKaryawanSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [Route("Karyawan/Settings")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user ID from session
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Sesi telah berakhir. Silakan login kembali.";
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                // Verify user is karyawan
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole?.ToLower() != "karyawan")
                {
                    TempData["ErrorMessage"] = "Akses tidak diizinkan.";
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                // Get user profile using service
                var profile = await _settingsService.GetKaryawanProfile(userId.Value);
                if (profile == null)
                {
                    TempData["ErrorMessage"] = "Data pengguna tidak ditemukan.";
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var viewModel = new KaryawanSettingsViewModel
                {
                    Profile = profile
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat halaman pengaturan.";
                return RedirectToAction("Index", "Home", new { area = "Karyawan" });
            }
        }

        [Route("Karyawan/Settings")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(KaryawanSettingsViewModel model, string action)
        {
            try
            {
                // Get current user ID from session
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "Sesi telah berakhir. Silakan login kembali.";
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                // Verify user is karyawan
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole?.ToLower() != "karyawan")
                {
                    TempData["ErrorMessage"] = "Akses tidak diizinkan.";
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                if (action == "updateProfile")
                {
                    return await UpdateProfileInternal(userId.Value, model.Profile);
                }
                else if (action == "changePassword")
                {
                    return await ChangePasswordInternal(userId.Value, model.ChangePassword);
                }

                TempData["ErrorMessage"] = "Aksi tidak valid.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memproses permintaan.";
                return RedirectToAction("Index");
            }
        }

        private async Task<IActionResult> UpdateProfileInternal(int userId, KaryawanProfileViewModel model)
        {
            try
            {
                // Validate model
                if (string.IsNullOrWhiteSpace(model.Nama) || string.IsNullOrWhiteSpace(model.Email))
                {
                    TempData["ErrorMessage"] = "Nama dan email wajib diisi.";
                    return RedirectToAction("Index");
                }

                // Update profile using service
                var result = await _settingsService.UpdateKaryawanProfile(userId, model);

                if (result.Success)
                {
                    // Update session data with new name
                    HttpContext.Session.SetString("Username", model.Nama);
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memperbarui profil.";
                return RedirectToAction("Index");
            }
        }

        private async Task<IActionResult> ChangePasswordInternal(int userId, ChangePasswordViewModel model)
        {
            try
            {
                // Validate model
                if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                    string.IsNullOrWhiteSpace(model.NewPassword) ||
                    string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
                {
                    TempData["ErrorMessage"] = "Semua field password wajib diisi.";
                    return RedirectToAction("Index");
                }

                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    TempData["ErrorMessage"] = "Konfirmasi password tidak sesuai.";
                    return RedirectToAction("Index");
                }

                if (model.NewPassword.Length < 6)
                {
                    TempData["ErrorMessage"] = "Password baru minimal 6 karakter.";
                    return RedirectToAction("Index");
                }

                // Change password using service
                var result = await _settingsService.ChangePassword(userId, model);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Password berhasil diubah.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengubah password.";
                return RedirectToAction("Index");
            }
        }
    }
}
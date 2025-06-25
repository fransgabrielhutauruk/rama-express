// Areas/Admin/Controllers/SettingsController.cs
using Microsoft.AspNetCore.Mvc;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [Route("Admin/Settings")]
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

                // Get user profile using service
                var profile = await _settingsService.GetAdminProfile(userId.Value);
                if (profile == null)
                {
                    TempData["ErrorMessage"] = "Data pengguna tidak ditemukan.";
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var viewModel = new SettingsViewModel
                {
                    Profile = profile
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat halaman pengaturan.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Route("Admin/Settings/ChangePassword")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
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

                if (!ModelState.IsValid)
                {
                    // Reload the settings page with validation errors
                    var profile = await _settingsService.GetAdminProfile(userId.Value);
                    if (profile != null)
                    {
                        var viewModel = new SettingsViewModel
                        {
                            Profile = profile,
                            ChangePassword = model
                        };
                        return View("Index", viewModel);
                    }
                    return RedirectToAction("Index");
                }

                // Change password using service
                var result = await _settingsService.ChangePassword(userId.Value, model);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Password berhasil diubah. Silakan login ulang dengan password baru.";

                    // Force logout for security
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "User", new { area = "" });
                }
                else
                {
                    // Add specific error to model state if it's about current password
                    if (result.Message.Contains("Password lama"))
                    {
                        ModelState.AddModelError("CurrentPassword", result.Message);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.Message;
                    }

                    // Reload the settings page with error
                    var profile = await _settingsService.GetAdminProfile(userId.Value);
                    if (profile != null)
                    {
                        var viewModel = new SettingsViewModel
                        {
                            Profile = profile,
                            ChangePassword = model
                        };
                        return View("Index", viewModel);
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengubah password. Silakan coba lagi.";
                return RedirectToAction("Index");
            }
        }

        [Route("Admin/Settings/Profile")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(AdminProfileViewModel model)
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

                // Update profile using service
                var result = await _settingsService.UpdateAdminProfile(userId.Value, model);

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
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memperbarui profil. Silakan coba lagi.";
                return RedirectToAction("Index");
            }
        }
    }
}
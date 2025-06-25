// Areas/Admin/Data/Service/SettingsService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public class SettingsService : ISettingsService
    {
        private readonly RamaExpressAppContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(RamaExpressAppContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AdminProfileViewModel?> GetAdminProfile(int userId)
        {
            try
            {
                var user = await _context.User
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return null;

                return new AdminProfileViewModel
                {
                    Id = user.Id,
                    Nama = user.Nama,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    IsActive = user.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin profile for user {UserId}", userId);
                return null;
            }
        }

        public async Task<(bool Success, string Message)> UpdateAdminProfile(int userId, AdminProfileViewModel model)
        {
            try
            {
                var user = await GetUserById(userId);
                if (user == null)
                {
                    return (false, "Data pengguna tidak ditemukan");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(model.Nama) || string.IsNullOrWhiteSpace(model.Email))
                {
                    return (false, "Nama dan email wajib diisi");
                }

                // Check if email is already used by another user
                if (await IsEmailExists(model.Email, userId))
                {
                    return (false, "Email sudah digunakan oleh pengguna lain");
                }

                // Update user data
                user.Nama = model.Nama.Trim();
                user.Email = model.Email.ToLower().Trim();
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated for user {UserId} at {Time}", userId, DateTime.Now);

                return (true, "Profil berhasil diperbarui");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return (false, "Terjadi kesalahan saat memperbarui profil");
            }
        }

        public async Task<(bool Success, string Message)> ChangePassword(int userId, ChangePasswordViewModel model)
        {
            try
            {
                var user = await GetUserById(userId);
                if (user == null)
                {
                    return (false, "Data pengguna tidak ditemukan");
                }

                // Verify current password
                if (!await VerifyCurrentPassword(userId, model.CurrentPassword))
                {
                    return (false, "Password lama tidak sesuai");
                }

                // Validate new password
                if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
                {
                    return (false, "Password baru minimal 6 karakter");
                }

                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    return (false, "Konfirmasi password tidak sesuai");
                }

                // Hash new password
                var hasher = new PasswordHasher<User>();
                user.Password = hasher.HashPassword(user, model.NewPassword);
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed for user {UserId} at {Time}", userId, DateTime.Now);

                return (true, "Password berhasil diubah");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return (false, "Terjadi kesalahan saat mengubah password");
            }
        }

        public async Task<bool> VerifyCurrentPassword(int userId, string currentPassword)
        {
            try
            {
                var user = await GetUserById(userId);
                if (user == null)
                    return false;

                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(user, user.Password, currentPassword);

                return result == PasswordVerificationResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsEmailExists(string email, int excludeUserId)
        {
            try
            {
                return await _context.User
                    .Where(u => u.Email.ToLower() == email.ToLower().Trim()
                               && u.Id != excludeUserId
                               && !u.IsDeleted)
                    .AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence for {Email}", email);
                return true; // Return true to be safe and prevent duplicate emails
            }
        }

        public async Task<User?> GetUserById(int userId)
        {
            try
            {
                return await _context.User
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id {UserId}", userId);
                return null;
            }
        }
    }
}
// Areas/Karyawan/Data/Service/KaryawanSettingsService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using RamaExpress.Areas.Karyawan.Models;

namespace RamaExpress.Areas.Karyawan.Data.Service
{
    public class KaryawanSettingsService : IKaryawanSettingsService
    {
        private readonly RamaExpressAppContext _context;
        private readonly ILogger<KaryawanSettingsService> _logger;

        public KaryawanSettingsService(RamaExpressAppContext context, ILogger<KaryawanSettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<KaryawanProfileViewModel?> GetKaryawanProfile(int userId)
        {
            try
            {
                var user = await _context.User
                    .Where(u => u.Id == userId && !u.IsDeleted && u.Role.ToLower() == "karyawan")
                    .FirstOrDefaultAsync();

                if (user == null)
                    return null;

                return new KaryawanProfileViewModel
                {
                    Id = user.Id,
                    Nama = user.Nama ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Posisi = user.Posisi,
                    Role = user.Role ?? "Karyawan",
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt ?? DateTime.Now,
                    IsActive = user.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting karyawan profile for user {UserId}", userId);
                return null;
            }
        }

        public async Task<ServiceResult<KaryawanProfileViewModel>> UpdateKaryawanProfile(int userId, KaryawanProfileViewModel model)
        {
            try
            {
                var user = await _context.User
                    .Where(u => u.Id == userId && !u.IsDeleted && u.Role.ToLower() == "karyawan")
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return new ServiceResult<KaryawanProfileViewModel>
                    {
                        Success = false,
                        Message = "Pengguna tidak ditemukan atau tidak memiliki akses.",
                        Data = null
                    };
                }

                // Check if email is already used by another user
                var emailExists = await _context.User
                    .AnyAsync(u => u.Email == model.Email && u.Id != userId && !u.IsDeleted);

                if (emailExists)
                {
                    return new ServiceResult<KaryawanProfileViewModel>
                    {
                        Success = false,
                        Message = "Email sudah digunakan oleh pengguna lain.",
                        Data = null
                    };
                }

                // Update user profile
                user.Nama = model.Nama.Trim();
                user.Email = model.Email.ToLower().Trim();
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var updatedProfile = await GetKaryawanProfile(userId);

                return new ServiceResult<KaryawanProfileViewModel>
                {
                    Success = true,
                    Message = "Profil berhasil diperbarui.",
                    Data = updatedProfile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating karyawan profile for user {UserId}", userId);
                return new ServiceResult<KaryawanProfileViewModel>
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat memperbarui profil.",
                    Data = null
                };
            }
        }

        public async Task<ServiceResult<string>> ChangePassword(int userId, ChangePasswordViewModel model)
        {
            try
            {
                var user = await _context.User
                    .Where(u => u.Id == userId && !u.IsDeleted && u.Role.ToLower() == "karyawan")
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return new ServiceResult<string>
                    {
                        Success = false,
                        Message = "Pengguna tidak ditemukan atau tidak memiliki akses.",
                        Data = null
                    };
                }

                // Verify current password
                var hasher = new PasswordHasher<User>();
                var verificationResult = hasher.VerifyHashedPassword(user, user.Password, model.CurrentPassword);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    return new ServiceResult<string>
                    {
                        Success = false,
                        Message = "Password saat ini tidak sesuai.",
                        Data = null
                    };
                }

                // Hash new password and update
                user.Password = hasher.HashPassword(user, model.NewPassword);
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return new ServiceResult<string>
                {
                    Success = true,
                    Message = "Password berhasil diubah.",
                    Data = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return new ServiceResult<string>
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat mengubah password.",
                    Data = null
                };
            }
        }
    }
}
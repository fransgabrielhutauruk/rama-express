// Areas/Admin/Data/Service/ISettingsService.cs
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface ISettingsService
    {
        Task<AdminProfileViewModel?> GetAdminProfile(int userId);
        Task<(bool Success, string Message)> UpdateAdminProfile(int userId, AdminProfileViewModel model);
        Task<(bool Success, string Message)> ChangePassword(int userId, ChangePasswordViewModel model);
        Task<bool> VerifyCurrentPassword(int userId, string currentPassword);
        Task<bool> IsEmailExists(string email, int excludeUserId);
        Task<User?> GetUserById(int userId);
    }
}
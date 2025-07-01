// Areas/Karyawan/Data/Service/IKaryawanSettingsService.cs
using RamaExpress.Areas.Karyawan.Models;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Karyawan.Data.Service
{
    public interface IKaryawanSettingsService
    {
        Task<KaryawanProfileViewModel?> GetKaryawanProfile(int userId);
        Task<ServiceResult<KaryawanProfileViewModel>> UpdateKaryawanProfile(int userId, KaryawanProfileViewModel model);
        Task<ServiceResult<string>> ChangePassword(int userId, ChangePasswordViewModel model);
    }
}
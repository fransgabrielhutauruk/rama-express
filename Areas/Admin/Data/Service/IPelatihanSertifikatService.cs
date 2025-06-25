// Areas/Admin/Data/Service/IPelatihanSertifikatService.cs
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IPelatihanSertifikatService
    {
        Task<PelatihanSertifikat?> GetByPelatihanId(int pelatihanId);
        Task<PelatihanSertifikat?> GetById(int id);
        Task<(bool Success, string Message, PelatihanSertifikat? Certificate)> Add(PelatihanSertifikat certificate);
        Task<(bool Success, string Message, PelatihanSertifikat? Certificate)> Update(PelatihanSertifikat certificate);
        Task<(bool Success, string Message)> Delete(int id);
        Task<string> GenerateCertificateNumber(int pelatihanId);
        Task<bool> ExistsByPelatihanId(int pelatihanId);
    }
}
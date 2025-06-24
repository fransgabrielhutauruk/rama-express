// Areas/Admin/Data/Service/IPelatihanService.cs
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IPelatihanService
    {
        Task<(IEnumerable<Pelatihan> Pelatihans, int TotalCount)> GetAll(int page = 1, int pageSize = 10);
        Task<(IEnumerable<Pelatihan> Pelatihans, int TotalCount)> GetAllWithSearch(
            int page = 1,
            int pageSize = 10,
            string searchTerm = null,
            string statusFilter = null);
        Task<Pelatihan?> GetById(int id);
        Task<(bool Success, string Message, Pelatihan? Pelatihan)> Add(Pelatihan pelatihan);
        Task<(bool Success, string Message, Pelatihan? Pelatihan)> Update(Pelatihan pelatihan);
        Task<(bool Success, string Message)> Delete(int id);
        Task<bool> Exists(int id);
        Task<bool> IsKodeExists(string kode, int? excludeId = null);
    }
}
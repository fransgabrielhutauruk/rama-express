using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IPelatihanMateriService
    {
        Task<IEnumerable<PelatihanMateri>> GetByPelatihanId(int pelatihanId);
        Task<PelatihanMateri> GetById(int id);
        Task Add(PelatihanMateri material);
        Task Update(PelatihanMateri material);
        Task Delete(int id);
        Task<int> GetNextUrutan(int pelatihanId);
        Task ReorderMaterials(int pelatihanId); // Add this line
    }
}
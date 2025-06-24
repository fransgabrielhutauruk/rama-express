// Areas/Admin/Data/Service/IPelatihanMateriService.cs
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IPelatihanMateriService
    {
        Task<IEnumerable<PelatihanMateri>> GetByPelatihanId(int pelatihanId);
        Task<PelatihanMateri?> GetById(int id);
        Task<(bool Success, string Message, PelatihanMateri? Materi)> Add(PelatihanMateri material);
        Task<(bool Success, string Message, PelatihanMateri? Materi)> Update(PelatihanMateri material);
        Task<(bool Success, string Message)> Delete(int id);
        Task<int> GetNextUrutan(int pelatihanId);
        Task<(bool Success, string Message)> ReorderMaterials(int pelatihanId);
        Task<(bool Success, string Message)> MoveUp(int id);
        Task<(bool Success, string Message)> MoveDown(int id);
    }
}
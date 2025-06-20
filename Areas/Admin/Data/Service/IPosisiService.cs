using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IPosisiService
    {
        Task<IEnumerable<Posisi>> GetAll();
        Task<Posisi?> GetById(int id);
        Task<(bool Success, string Message, Posisi? Posisi)> Add(Posisi posisi);
        Task<(bool Success, string Message, Posisi? Posisi)> Update(Posisi posisi);
        Task<(bool Success, string Message)> Delete(int id);

        Task<IEnumerable<Posisi>> GetActivePosisi();

        Task<IEnumerable<PosisiWithCountViewModel>> GetPosisiWithEmployeeCount();
    }
}
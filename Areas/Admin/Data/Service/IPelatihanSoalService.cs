// Areas/Admin/Data/Service/IPelatihanSoalService.cs
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IPelatihanSoalService
    {
        Task<IEnumerable<PelatihanSoal>> GetByPelatihanId(int pelatihanId);
        Task<PelatihanSoal?> GetById(int id);
        Task<(bool Success, string Message, PelatihanSoal? Soal)> Add(PelatihanSoal soal);
        Task<(bool Success, string Message, PelatihanSoal? Soal)> Update(PelatihanSoal soal);
        Task<(bool Success, string Message)> Delete(int id);
        Task<int> GetNextUrutan(int pelatihanId);
        Task<(bool Success, string Message)> ReorderQuestions(int pelatihanId);
        Task<(bool Success, string Message)> MoveUp(int id);
        Task<(bool Success, string Message)> MoveDown(int id);
    }
}
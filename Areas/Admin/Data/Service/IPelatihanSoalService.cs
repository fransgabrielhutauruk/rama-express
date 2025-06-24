// Make sure your IPelatihanSoalService.cs interface includes all these methods:

using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IPelatihanSoalService
    {
        Task<IEnumerable<PelatihanSoal>> GetByPelatihanId(int pelatihanId);
        Task<PelatihanSoal> GetById(int id);
        Task Add(PelatihanSoal soal);
        Task Update(PelatihanSoal soal);
        Task Delete(int id);
        Task<int> GetNextUrutan(int pelatihanId);
        Task UpdateOrder(int soalId, int newOrder); // Make sure this exists
        Task ReorderQuestions(int pelatihanId); // Make sure this exists
    }
}
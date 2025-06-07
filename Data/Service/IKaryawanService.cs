using RamaExpress.Models;

namespace RamaExpress.Data.Service
{
    public interface IKaryawanService
    {
        Task<IEnumerable<Karyawan>> GetAll();
        Task Add(Karyawan karyawan);
    }
}

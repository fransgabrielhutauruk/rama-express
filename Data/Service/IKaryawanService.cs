using RamaExpress.Models;

namespace RamaExpress.Data.Service
{
    public interface IKaryawanService
    {
        Task<IEnumerable<User>> GetAll();
        Task Add(User user);
    }
}

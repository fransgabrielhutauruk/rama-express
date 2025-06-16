using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IKaryawanService
    {
        Task<(IEnumerable<User> Users, int TotalCount)> GetAll(int page = 1, int pageSize = 10);

        Task<(IEnumerable<User> Users, int TotalCount)> GetAllWithSearch(
            int page = 1,
            int pageSize = 10,
            string searchTerm = null,
            string statusFilter = null);

        Task<(IEnumerable<User> Users, int TotalCount)> GetAllWithSearchAndSort(
            int page = 1,
            int pageSize = 10,
            string searchTerm = null,
            string statusFilter = null,
            string sortField = "Nama",
            string sortDirection = "asc");

        Task Add(Models.User user);
    }
}
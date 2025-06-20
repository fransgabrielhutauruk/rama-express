using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public interface IKaryawanService
    {
        Task<(IEnumerable<User> Users, int TotalCount)> GetAll(int page = 1, int pageSize = 10);

        Task<(IEnumerable<User> Users, int TotalCount)> GetAllWithSearch(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? statusFilter = null);

        Task<(IEnumerable<User> Users, int TotalCount)> GetAllWithSearchAndSort(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? statusFilter = null,
            string sortField = "Nama",
            string sortDirection = "asc");

        Task<(bool Success, string Message, User? User)> AddKaryawan(User karyawan);
        Task<bool> IsEmailExists(string email, int? excludeId = null);
        Task<User?> GetById(int id);    
        Task<(bool Success, string Message, User? User)> UpdateKaryawan(User karyawan);
        Task<(bool Success, string Message)> DeleteKaryawan(int id);
        Task<(bool Success, string Message)> ToggleActiveStatus(int id);
    }
}
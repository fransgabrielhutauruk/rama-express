using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public class KaryawanService : IKaryawanService
    {
        private readonly RamaExpressAppContext _context;

        public KaryawanService(RamaExpressAppContext context)
        {
            _context = context;
        }

        public async Task Add(Models.User karyawan)
        {
            _context.User.Add(karyawan);
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<Models.User> Users, int TotalCount)> GetAll(int page = 1, int pageSize = 15)
        {
            var query = _context.User
                .Where(u => u.Role != null && u.Role.ToLower() == "karyawan");

            var totalCount = await query.CountAsync();

            var karyawan = await query
                .OrderBy(u => u.Nama)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (karyawan, totalCount);
        }

        public async Task<(IEnumerable<Models.User> Users, int TotalCount)> GetAllWithSearch(
            int page = 1,
            int pageSize = 10,
            string searchTerm = null,
            string statusFilter = null)
        {
            var query = _context.User
                .Where(u => u.Role != null && u.Role.ToLower() == "karyawan");

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Nama.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool isActive = statusFilter.ToLower() == "aktif";
                query = query.Where(u => u.IsActive == isActive);
            }

            var totalCount = await query.CountAsync();

            var karyawan = await query
                .OrderBy(u => u.Nama)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (karyawan, totalCount);
        }

        public async Task<(IEnumerable<Models.User> Users, int TotalCount)> GetAllWithSearchAndSort(
            int page = 1,
            int pageSize = 10,
            string searchTerm = null,
            string statusFilter = null,
            string sortField = "Nama",
            string sortDirection = "asc")
        {
            var query = _context.User
                .Where(u => u.Role != null && u.Role.ToLower() == "karyawan");

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Nama.Contains(searchTerm) ||
                                        u.Email.Contains(searchTerm) ||
                                        u.Posisi.Contains(searchTerm));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool isActive = statusFilter.ToLower() == "aktif";
                query = query.Where(u => u.IsActive == isActive);
            }

            // Apply sorting
            query = ApplySorting(query, sortField, sortDirection);

            var totalCount = await query.CountAsync();

            var karyawan = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (karyawan, totalCount);
        }

        private IQueryable<Models.User> ApplySorting(IQueryable<Models.User> query, string sortField, string sortDirection)
        {
            // Validate sort field
            var validSortFields = new[] { "Nama", "Email", "Posisi", "CreatedAt", "IsActive" };
            if (!validSortFields.Contains(sortField))
                sortField = "Nama";

            // Validate sort direction
            if (sortDirection != "asc" && sortDirection != "desc")
                sortDirection = "asc";

            return sortField.ToLower() switch
            {
                "nama" => sortDirection == "asc"
                    ? query.OrderBy(u => u.Nama)
                    : query.OrderByDescending(u => u.Nama),

                "email" => sortDirection == "asc"
                    ? query.OrderBy(u => u.Email)
                    : query.OrderByDescending(u => u.Email),

                "posisi" => sortDirection == "asc"
                    ? query.OrderBy(u => u.Posisi)
                    : query.OrderByDescending(u => u.Posisi),

                "createdat" => sortDirection == "asc"
                    ? query.OrderBy(u => u.CreatedAt)
                    : query.OrderByDescending(u => u.CreatedAt),

                "isactive" => sortDirection == "asc"
                    ? query.OrderBy(u => u.IsActive)
                    : query.OrderByDescending(u => u.IsActive),

                _ => query.OrderBy(u => u.Nama)
            };
        }
    }
}
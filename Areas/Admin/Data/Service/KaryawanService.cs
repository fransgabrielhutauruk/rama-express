using Microsoft.AspNetCore.Identity;
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
                .Where(u => u.Role != null && u.Role.ToLower() == "karyawan" && !u.IsDeleted);

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
                .Where(u => u.Role != null && u.Role.ToLower() == "karyawan" && !u.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Nama.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

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
                .Where(u => u.Role != null && u.Role.ToLower() == "karyawan" && !u.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Nama.Contains(searchTerm) ||
                                        u.Email.Contains(searchTerm) ||
                                        u.Posisi.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool isActive = statusFilter.ToLower() == "aktif";
                query = query.Where(u => u.IsActive == isActive);
            }

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
            var validSortFields = new[] { "Nama", "Email", "Posisi", "CreatedAt", "IsActive" };
            if (!validSortFields.Contains(sortField))
                sortField = "Nama";

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

        public async Task<(bool Success, string Message, User? User)> AddKaryawan(User karyawan)
        {
            try
            {
                if (await IsEmailExists(karyawan.Email))
                {
                    return (false, "Email sudah digunakan oleh pengguna lain", null);
                }

                var hasher = new PasswordHasher<User>();
                karyawan.Password = hasher.HashPassword(karyawan, karyawan.Password);

                karyawan.Role = "karyawan";
                karyawan.CreatedAt = DateTime.Now;
                karyawan.IsActive = true;
                karyawan.IsDeleted = false;
                karyawan.UpdatedAt = null;
                karyawan.DeletedAt = null;

                karyawan.Email = karyawan.Email.ToLower().Trim();
                karyawan.Nama = karyawan.Nama.Trim();
                if (!string.IsNullOrEmpty(karyawan.Posisi))
                {
                    karyawan.Posisi = karyawan.Posisi.Trim();
                }

                _context.User.Add(karyawan);
                await _context.SaveChangesAsync();

                return (true, $"Karyawan {karyawan.Nama} berhasil ditambahkan", karyawan);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<bool> IsEmailExists(string email, int? excludeId = null)
        {
            var query = _context.User
                .Where(u => u.Email.ToLower() == email.ToLower().Trim() && !u.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(u => u.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<User?> GetById(int id)
        {
            return await _context.User
                .Where(u => u.Id == id && u.Role.ToLower() == "karyawan" && !u.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, User? User)> UpdateKaryawan(User karyawan)
        {
            try
            {
                var existingUser = await GetById(karyawan.Id);
                if (existingUser == null)
                {
                    return (false, "Karyawan tidak ditemukan", null);
                }

                if (await IsEmailExists(karyawan.Email, karyawan.Id))
                {
                    return (false, "Email sudah digunakan oleh pengguna lain", null);
                }

                existingUser.Nama = karyawan.Nama.Trim();
                existingUser.Email = karyawan.Email.ToLower().Trim();
                existingUser.Posisi = string.IsNullOrEmpty(karyawan.Posisi) ? null : karyawan.Posisi.Trim();
                existingUser.UpdatedAt = DateTime.Now;

                if (!string.IsNullOrEmpty(karyawan.Password))
                {
                    var hasher = new PasswordHasher<User>();
                    existingUser.Password = hasher.HashPassword(existingUser, karyawan.Password);
                }

                await _context.SaveChangesAsync();

                return (true, $"Data karyawan {existingUser.Nama} berhasil diperbarui", existingUser);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteKaryawan(int id)
        {
            try
            {
                var user = await GetById(id);
                if (user == null)
                {
                    return (false, "Karyawan tidak ditemukan");
                }

                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return (true, $"Karyawan {user.Nama} berhasil dihapus");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleActiveStatus(int id)
        {
            try
            {
                var user = await GetById(id);
                if (user == null)
                {
                    return (false, "Karyawan tidak ditemukan");
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                string status = user.IsActive ? "diaktifkan" : "dinonaktifkan";
                return (true, $"Karyawan {user.Nama} berhasil {status}");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }
    }
}
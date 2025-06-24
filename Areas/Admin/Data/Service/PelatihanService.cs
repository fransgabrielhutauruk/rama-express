// Areas/Admin/Data/Service/PelatihanService.cs
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public class PelatihanService : IPelatihanService
    {
        private readonly RamaExpressAppContext _context;

        public PelatihanService(RamaExpressAppContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Pelatihan> Pelatihans, int TotalCount)> GetAll(int page = 1, int pageSize = 10)
        {
            var query = _context.Pelatihan.Where(p => !p.IsDeleted);

            var totalCount = await query.CountAsync();

            var pelatihans = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (pelatihans, totalCount);
        }

        public async Task<(IEnumerable<Pelatihan> Pelatihans, int TotalCount)> GetAllWithSearch(
            int page = 1,
            int pageSize = 10,
            string searchTerm = null,
            string statusFilter = null)
        {
            var query = _context.Pelatihan.Where(p => !p.IsDeleted);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Judul.Contains(searchTerm) ||
                                        p.Deskripsi.Contains(searchTerm) ||
                                        p.Kode.Contains(searchTerm));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool isActive = statusFilter.ToLower() == "aktif";
                query = query.Where(p => p.IsActive == isActive);
            }

            var totalCount = await query.CountAsync();

            var pelatihans = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (pelatihans, totalCount);
        }

        public async Task<Pelatihan?> GetById(int id)
        {
            return await _context.Pelatihan
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<(bool Success, string Message, Pelatihan? Pelatihan)> Add(Pelatihan pelatihan)
        {
            try
            {
                // Validate unique code
                if (await IsKodeExists(pelatihan.Kode))
                {
                    return (false, $"Kode pelatihan '{pelatihan.Kode}' sudah digunakan", null);
                }

                // Set default values
                pelatihan.CreatedAt = DateTime.Now;
                pelatihan.IsActive = true;
                pelatihan.IsDeleted = false;
                pelatihan.UpdatedAt = null;
                pelatihan.DeletedAt = null;

                // Normalize data
                pelatihan.Kode = pelatihan.Kode.Trim().ToUpper();
                pelatihan.Judul = pelatihan.Judul.Trim();
                if (!string.IsNullOrEmpty(pelatihan.Deskripsi))
                {
                    pelatihan.Deskripsi = pelatihan.Deskripsi.Trim();
                }

                _context.Pelatihan.Add(pelatihan);
                await _context.SaveChangesAsync();

                return (true, $"Pelatihan '{pelatihan.Judul}' berhasil ditambahkan", pelatihan);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, Pelatihan? Pelatihan)> Update(Pelatihan pelatihan)
        {
            try
            {
                var existingPelatihan = await GetById(pelatihan.Id);
                if (existingPelatihan == null)
                {
                    return (false, "Pelatihan tidak ditemukan", null);
                }

                // Validate unique code (exclude current record)
                if (await IsKodeExists(pelatihan.Kode, pelatihan.Id))
                {
                    return (false, $"Kode pelatihan '{pelatihan.Kode}' sudah digunakan oleh pelatihan lain", null);
                }

                // Update fields
                existingPelatihan.Kode = pelatihan.Kode.Trim().ToUpper();
                existingPelatihan.Judul = pelatihan.Judul.Trim();
                existingPelatihan.Deskripsi = string.IsNullOrEmpty(pelatihan.Deskripsi) ? null : pelatihan.Deskripsi.Trim();
                existingPelatihan.DurasiMenit = pelatihan.DurasiMenit;
                existingPelatihan.SkorMinimal = pelatihan.SkorMinimal;
                existingPelatihan.IsActive = pelatihan.IsActive;
                existingPelatihan.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return (true, $"Pelatihan '{existingPelatihan.Judul}' berhasil diperbarui", existingPelatihan);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> Delete(int id)
        {
            try
            {
                var pelatihan = await GetById(id);
                if (pelatihan == null)
                {
                    return (false, "Pelatihan tidak ditemukan");
                }

                // Check if pelatihan has any related data
                var hasMateri = await _context.PelatihanMateri.AnyAsync(pm => pm.PelatihanId == id);
                var hasSoal = await _context.PelatihanSoal.AnyAsync(ps => ps.PelatihanId == id);
                var hasProgress = await _context.PelatihanProgress.AnyAsync(pp => pp.PelatihanId == id);

                if (hasMateri || hasSoal || hasProgress)
                {
                    return (false, $"Pelatihan '{pelatihan.Judul}' tidak dapat dihapus karena memiliki data terkait (materi, soal, atau progress karyawan)");
                }

                // Soft delete
                pelatihan.IsDeleted = true;
                pelatihan.DeletedAt = DateTime.Now;
                pelatihan.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return (true, $"Pelatihan '{pelatihan.Judul}' berhasil dihapus");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        public async Task<bool> Exists(int id)
        {
            return await _context.Pelatihan.AnyAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<bool> IsKodeExists(string kode, int? excludeId = null)
        {
            var query = _context.Pelatihan
                .Where(p => p.Kode.ToLower() == kode.ToLower().Trim() && !p.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
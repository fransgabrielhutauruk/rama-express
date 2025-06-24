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

        public async Task<Pelatihan> GetById(int id)
        {
            return await _context.Pelatihan
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task Add(Pelatihan pelatihan)
        {
            _context.Pelatihan.Add(pelatihan);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Pelatihan pelatihan)
        {
            _context.Pelatihan.Update(pelatihan);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var pelatihan = await GetById(id);
            if (pelatihan != null)
            {
                pelatihan.IsDeleted = true;
                pelatihan.DeletedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> Exists(int id)
        {
            return await _context.Pelatihan.AnyAsync(p => p.Id == id && !p.IsDeleted);
        }
    }
}
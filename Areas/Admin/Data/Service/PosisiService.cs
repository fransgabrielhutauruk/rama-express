using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public class PosisiService : IPosisiService
    {
        private readonly RamaExpressAppContext _context;

        public PosisiService(RamaExpressAppContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Posisi>> GetAll()
        {
            return await _context.Posisi
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Posisi?> GetById(int id)
        {
            return await _context.Posisi
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, Posisi? Posisi)> Add(Posisi posisi)
        {
            try
            {
                posisi.Name = posisi.Name.Trim();
                posisi.IsDeleted = false;
                posisi.DeletedAt = null;

                _context.Posisi.Add(posisi);
                await _context.SaveChangesAsync();

                return (true, $"Posisi '{posisi.Name}' berhasil ditambahkan", posisi);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, Posisi? Posisi)> Update(Posisi posisi)
        {
            try
            {
                var existingPosisi = await GetById(posisi.Id);
                if (existingPosisi == null)
                {
                    return (false, "Posisi tidak ditemukan", null);
                }

                existingPosisi.Name = posisi.Name.Trim();

                await _context.SaveChangesAsync();

                return (true, $"Posisi '{existingPosisi.Name}' berhasil diperbarui", existingPosisi);
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
                var posisi = await GetById(id);
                if (posisi == null)
                {
                    return (false, "Posisi tidak ditemukan");
                }

                var employeeCount = await _context.User
                    .Where(u => u.Posisi == posisi.Name && !u.IsDeleted)
                    .CountAsync();

                if (employeeCount > 0)
                {
                    return (false, $"Posisi '{posisi.Name}' tidak dapat dihapus karena masih digunakan oleh {employeeCount} karyawan");
                }

                posisi.IsDeleted = true;
                posisi.DeletedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return (true, $"Posisi '{posisi.Name}' berhasil dihapus");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        public async Task<IEnumerable<Posisi>> GetActivePosisi()
        {
            return await _context.Posisi
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<PosisiWithCountViewModel>> GetPosisiWithEmployeeCount()
        {
            var posisiList = await GetAll();
            var result = new List<PosisiWithCountViewModel>();

            foreach (var posisi in posisiList)
            {
                var employeeCount = await _context.User
                    .Where(u => u.Posisi == posisi.Name && !u.IsDeleted)
                    .CountAsync();

                result.Add(new PosisiWithCountViewModel(posisi, employeeCount));
            }

            return result;
        }
    }
}
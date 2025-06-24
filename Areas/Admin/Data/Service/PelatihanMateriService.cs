// Areas/Admin/Data/Service/PelatihanMateriService.cs
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public class PelatihanMateriService : IPelatihanMateriService
    {
        private readonly RamaExpressAppContext _context;

        public PelatihanMateriService(RamaExpressAppContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PelatihanMateri>> GetByPelatihanId(int pelatihanId)
        {
            return await _context.PelatihanMateri
                .Where(m => m.PelatihanId == pelatihanId)
                .OrderBy(m => m.Urutan)
                .ToListAsync();
        }

        public async Task<PelatihanMateri?> GetById(int id)
        {
            return await _context.PelatihanMateri
                .Include(m => m.Pelatihan)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<(bool Success, string Message, PelatihanMateri? Materi)> Add(PelatihanMateri material)
        {
            try
            {
                // Validate pelatihan exists
                var pelatihanExists = await _context.Pelatihan.AnyAsync(p => p.Id == material.PelatihanId && !p.IsDeleted);
                if (!pelatihanExists)
                {
                    return (false, "Pelatihan tidak ditemukan", null);
                }

                // Get next order number
                var maxOrder = await _context.PelatihanMateri
                    .Where(m => m.PelatihanId == material.PelatihanId)
                    .MaxAsync(m => (int?)m.Urutan) ?? 0;

                material.Urutan = maxOrder + 1;
                material.CreatedAt = DateTime.Now;
                material.UpdatedAt = null;

                // Normalize data
                material.Judul = material.Judul.Trim();
                if (!string.IsNullOrEmpty(material.Konten))
                {
                    material.Konten = material.Konten.Trim();
                }

                _context.PelatihanMateri.Add(material);
                await _context.SaveChangesAsync();

                return (true, $"Materi '{material.Judul}' berhasil ditambahkan", material);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, PelatihanMateri? Materi)> Update(PelatihanMateri material)
        {
            try
            {
                var existingMaterial = await _context.PelatihanMateri.FindAsync(material.Id);
                if (existingMaterial == null)
                {
                    return (false, "Materi tidak ditemukan", null);
                }

                // Update properties
                existingMaterial.Judul = material.Judul.Trim();
                existingMaterial.Konten = string.IsNullOrEmpty(material.Konten) ? null : material.Konten.Trim();
                existingMaterial.TipeKonten = material.TipeKonten;
                existingMaterial.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return (true, $"Materi '{existingMaterial.Judul}' berhasil diperbarui", existingMaterial);
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
                var material = await _context.PelatihanMateri.FindAsync(id);
                if (material == null)
                {
                    return (false, "Materi tidak ditemukan");
                }

                var pelatihanId = material.PelatihanId;
                var judulMateri = material.Judul;

                // Delete the material
                _context.PelatihanMateri.Remove(material);
                await _context.SaveChangesAsync();

                // Reorder all remaining materials to ensure sequential numbering
                var reorderResult = await ReorderMaterials(pelatihanId);
                if (!reorderResult.Success)
                {
                    return (false, $"Materi '{judulMateri}' dihapus tetapi gagal mengurutkan ulang: {reorderResult.Message}");
                }

                return (true, $"Materi '{judulMateri}' berhasil dihapus");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        public async Task<int> GetNextUrutan(int pelatihanId)
        {
            var maxOrder = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == pelatihanId)
                .MaxAsync(m => (int?)m.Urutan) ?? 0;

            return maxOrder + 1;
        }

        public async Task<(bool Success, string Message)> ReorderMaterials(int pelatihanId)
        {
            try
            {
                var materials = await _context.PelatihanMateri
                    .Where(m => m.PelatihanId == pelatihanId)
                    .OrderBy(m => m.Urutan)
                    .ToListAsync();

                for (int i = 0; i < materials.Count; i++)
                {
                    materials[i].Urutan = i + 1; // Start from 1
                    materials[i].UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return (true, "Urutan materi berhasil diperbarui");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan saat mengurutkan ulang materi: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> MoveUp(int id)
        {
            try
            {
                var material = await GetById(id);
                if (material == null)
                {
                    return (false, "Materi tidak ditemukan");
                }

                // Find the material above this one
                var materials = await GetByPelatihanId(material.PelatihanId);
                var currentMaterial = materials.FirstOrDefault(m => m.Id == id);
                var previousMaterial = materials
                    .Where(m => m.Urutan < currentMaterial.Urutan)
                    .OrderByDescending(m => m.Urutan)
                    .FirstOrDefault();

                if (previousMaterial == null)
                {
                    return (false, "Materi sudah berada di urutan teratas");
                }

                // Swap the order
                var tempOrder = currentMaterial.Urutan;
                currentMaterial.Urutan = previousMaterial.Urutan;
                previousMaterial.Urutan = tempOrder;

                // Update both materials
                _context.PelatihanMateri.Update(currentMaterial);
                _context.PelatihanMateri.Update(previousMaterial);
                await _context.SaveChangesAsync();

                return (true, "Urutan materi berhasil diubah");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> MoveDown(int id)
        {
            try
            {
                var material = await GetById(id);
                if (material == null)
                {
                    return (false, "Materi tidak ditemukan");
                }

                // Find the material below this one
                var materials = await GetByPelatihanId(material.PelatihanId);
                var currentMaterial = materials.FirstOrDefault(m => m.Id == id);
                var nextMaterial = materials
                    .Where(m => m.Urutan > currentMaterial.Urutan)
                    .OrderBy(m => m.Urutan)
                    .FirstOrDefault();

                if (nextMaterial == null)
                {
                    return (false, "Materi sudah berada di urutan terbawah");
                }

                // Swap the order
                var tempOrder = currentMaterial.Urutan;
                currentMaterial.Urutan = nextMaterial.Urutan;
                nextMaterial.Urutan = tempOrder;

                // Update both materials
                _context.PelatihanMateri.Update(currentMaterial);
                _context.PelatihanMateri.Update(nextMaterial);
                await _context.SaveChangesAsync();

                return (true, "Urutan materi berhasil diubah");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }
    }
}
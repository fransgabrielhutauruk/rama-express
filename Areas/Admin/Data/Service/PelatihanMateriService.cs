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

        public async Task<PelatihanMateri> GetById(int id)
        {
            return await _context.PelatihanMateri
                .Include(m => m.Pelatihan)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task Add(PelatihanMateri material)
        {
            // Get next order number
            var maxOrder = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == material.PelatihanId)
                .MaxAsync(m => (int?)m.Urutan) ?? 0;

            material.Urutan = maxOrder + 1;
            material.CreatedAt = DateTime.Now;

            _context.PelatihanMateri.Add(material);
            await _context.SaveChangesAsync();
        }

        public async Task ReorderMaterials(int pelatihanId)
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
        }

        // Updated Delete method using the reorder helper
        public async Task Delete(int id)
        {
            var material = await _context.PelatihanMateri.FindAsync(id);
            if (material != null)
            {
                var pelatihanId = material.PelatihanId;

                // Delete the material
                _context.PelatihanMateri.Remove(material);
                await _context.SaveChangesAsync();

                // Reorder all remaining materials to ensure sequential numbering
                await ReorderMaterials(pelatihanId);
            }
        }

        public async Task<int> GetNextUrutan(int pelatihanId)
        {
            var maxOrder = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == pelatihanId)
                .MaxAsync(m => (int?)m.Urutan) ?? 0;

            return maxOrder + 1;
        }

        public async Task UpdateOrder(int materialId, int newOrder)
        {
            var material = await _context.PelatihanMateri.FindAsync(materialId);
            if (material != null)
            {
                material.Urutan = newOrder;
                material.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        // Also update the Update method to handle order changes better
        public async Task Update(PelatihanMateri material)
        {
            var existingMaterial = await _context.PelatihanMateri.FindAsync(material.Id);
            if (existingMaterial == null)
            {
                throw new ArgumentException("Material not found");
            }

            // Update properties
            existingMaterial.Judul = material.Judul;
            existingMaterial.Konten = material.Konten;
            existingMaterial.TipeKonten = material.TipeKonten;
            existingMaterial.Urutan = material.Urutan; // Add this line
            existingMaterial.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}
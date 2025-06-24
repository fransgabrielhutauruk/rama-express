// Update your PelatihanSoalService.cs - Replace the entire file

using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public class PelatihanSoalService : IPelatihanSoalService
    {
        private readonly RamaExpressAppContext _context;

        public PelatihanSoalService(RamaExpressAppContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PelatihanSoal>> GetByPelatihanId(int pelatihanId)
        {
            return await _context.PelatihanSoal
                .Where(q => q.PelatihanId == pelatihanId)
                .OrderBy(q => q.Urutan)
                .ToListAsync();
        }

        public async Task<PelatihanSoal> GetById(int id)
        {
            return await _context.PelatihanSoal
                .Include(q => q.Pelatihan)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task Add(PelatihanSoal soal)
        {
            // Get next order number
            var maxOrder = await _context.PelatihanSoal
                .Where(s => s.PelatihanId == soal.PelatihanId)
                .MaxAsync(s => (int?)s.Urutan) ?? 0;

            soal.Urutan = maxOrder + 1;
            soal.CreatedAt = DateTime.Now;

            _context.PelatihanSoal.Add(soal);
            await _context.SaveChangesAsync();
        }

        public async Task Update(PelatihanSoal soal)
        {
            var existingSoal = await _context.PelatihanSoal.FindAsync(soal.Id);
            if (existingSoal == null)
            {
                throw new ArgumentException("Soal not found");
            }

            // Update properties
            existingSoal.Pertanyaan = soal.Pertanyaan;
            existingSoal.OpsiA = soal.OpsiA;
            existingSoal.OpsiB = soal.OpsiB;
            existingSoal.OpsiC = soal.OpsiC;
            existingSoal.OpsiD = soal.OpsiD;
            existingSoal.JawabanBenar = soal.JawabanBenar;
            existingSoal.Urutan = soal.Urutan;

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var soal = await _context.PelatihanSoal.FindAsync(id);
            if (soal != null)
            {
                var pelatihanId = soal.PelatihanId;

                // Delete the question
                _context.PelatihanSoal.Remove(soal);
                await _context.SaveChangesAsync();

                // Reorder all remaining questions to ensure sequential numbering
                await ReorderQuestions(pelatihanId);
            }
        }

        public async Task<int> GetNextUrutan(int pelatihanId)
        {
            var maxOrder = await _context.PelatihanSoal
                .Where(s => s.PelatihanId == pelatihanId)
                .MaxAsync(s => (int?)s.Urutan) ?? 0;

            return maxOrder + 1;
        }

        public async Task UpdateOrder(int soalId, int newOrder)
        {
            var soal = await _context.PelatihanSoal.FindAsync(soalId);
            if (soal != null)
            {
                soal.Urutan = newOrder;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReorderQuestions(int pelatihanId)
        {
            var questions = await _context.PelatihanSoal
                .Where(q => q.PelatihanId == pelatihanId)
                .OrderBy(q => q.Urutan)
                .ToListAsync();

            for (int i = 0; i < questions.Count; i++)
            {
                questions[i].Urutan = i + 1;
            }

            await _context.SaveChangesAsync();
        }
    }
}
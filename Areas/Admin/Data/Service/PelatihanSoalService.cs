// Areas/Admin/Data/Service/PelatihanSoalService.cs
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

        public async Task<PelatihanSoal?> GetById(int id)
        {
            return await _context.PelatihanSoal
                .Include(q => q.Pelatihan)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<(bool Success, string Message, PelatihanSoal? Soal)> Add(PelatihanSoal soal)
        {
            try
            {
                // Validate pelatihan exists
                var pelatihanExists = await _context.Pelatihan.AnyAsync(p => p.Id == soal.PelatihanId && !p.IsDeleted);
                if (!pelatihanExists)
                {
                    return (false, "Pelatihan tidak ditemukan", null);
                }

                // Validate answer option
                var validAnswers = new[] { "A", "B", "C", "D" };
                if (!validAnswers.Contains(soal.JawabanBenar.ToUpper()))
                {
                    return (false, "Jawaban benar harus berupa A, B, C, atau D", null);
                }

                // Get next order number
                var maxOrder = await _context.PelatihanSoal
                    .Where(s => s.PelatihanId == soal.PelatihanId)
                    .MaxAsync(s => (int?)s.Urutan) ?? 0;

                soal.Urutan = maxOrder + 1;
                soal.CreatedAt = DateTime.Now;

                // Normalize data
                soal.Pertanyaan = soal.Pertanyaan.Trim();
                soal.OpsiA = soal.OpsiA.Trim();
                soal.OpsiB = soal.OpsiB.Trim();
                soal.OpsiC = soal.OpsiC.Trim();
                soal.OpsiD = soal.OpsiD.Trim();
                soal.JawabanBenar = soal.JawabanBenar.ToUpper();

                _context.PelatihanSoal.Add(soal);
                await _context.SaveChangesAsync();

                return (true, $"Soal nomor {soal.Urutan} berhasil ditambahkan", soal);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, PelatihanSoal? Soal)> Update(PelatihanSoal soal)
        {
            try
            {
                var existingSoal = await _context.PelatihanSoal.FindAsync(soal.Id);
                if (existingSoal == null)
                {
                    return (false, "Soal tidak ditemukan", null);
                }

                // Validate answer option
                var validAnswers = new[] { "A", "B", "C", "D" };
                if (!validAnswers.Contains(soal.JawabanBenar.ToUpper()))
                {
                    return (false, "Jawaban benar harus berupa A, B, C, atau D", null);
                }

                // Update properties
                existingSoal.Pertanyaan = soal.Pertanyaan.Trim();
                existingSoal.OpsiA = soal.OpsiA.Trim();
                existingSoal.OpsiB = soal.OpsiB.Trim();
                existingSoal.OpsiC = soal.OpsiC.Trim();
                existingSoal.OpsiD = soal.OpsiD.Trim();
                existingSoal.JawabanBenar = soal.JawabanBenar.ToUpper();

                await _context.SaveChangesAsync();

                return (true, $"Soal nomor {existingSoal.Urutan} berhasil diperbarui", existingSoal);
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
                var soal = await _context.PelatihanSoal.FindAsync(id);
                if (soal == null)
                {
                    return (false, "Soal tidak ditemukan");
                }

                var pelatihanId = soal.PelatihanId;
                var urutanSoal = soal.Urutan;

                // Delete the question
                _context.PelatihanSoal.Remove(soal);
                await _context.SaveChangesAsync();

                // Reorder all remaining questions to ensure sequential numbering
                var reorderResult = await ReorderQuestions(pelatihanId);
                if (!reorderResult.Success)
                {
                    return (false, $"Soal nomor {urutanSoal} dihapus tetapi gagal mengurutkan ulang: {reorderResult.Message}");
                }

                return (true, $"Soal nomor {urutanSoal} berhasil dihapus");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        public async Task<int> GetNextUrutan(int pelatihanId)
        {
            var maxOrder = await _context.PelatihanSoal
                .Where(s => s.PelatihanId == pelatihanId)
                .MaxAsync(s => (int?)s.Urutan) ?? 0;

            return maxOrder + 1;
        }

        public async Task<(bool Success, string Message)> ReorderQuestions(int pelatihanId)
        {
            try
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
                return (true, "Urutan soal berhasil diperbarui");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan saat mengurutkan ulang soal: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> MoveUp(int id)
        {
            try
            {
                var question = await GetById(id);
                if (question == null)
                {
                    return (false, "Soal tidak ditemukan");
                }

                // Find the question above this one
                var questions = await GetByPelatihanId(question.PelatihanId);
                var currentQuestion = questions.FirstOrDefault(q => q.Id == id);
                var previousQuestion = questions
                    .Where(q => q.Urutan < currentQuestion.Urutan)
                    .OrderByDescending(q => q.Urutan)
                    .FirstOrDefault();

                if (previousQuestion == null)
                {
                    return (false, "Soal sudah berada di urutan teratas");
                }

                // Swap the order
                var tempOrder = currentQuestion.Urutan;
                currentQuestion.Urutan = previousQuestion.Urutan;
                previousQuestion.Urutan = tempOrder;

                // Update both questions
                _context.PelatihanSoal.Update(currentQuestion);
                _context.PelatihanSoal.Update(previousQuestion);
                await _context.SaveChangesAsync();

                return (true, "Urutan soal berhasil diubah");
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
                var question = await GetById(id);
                if (question == null)
                {
                    return (false, "Soal tidak ditemukan");
                }

                // Find the question below this one
                var questions = await GetByPelatihanId(question.PelatihanId);
                var currentQuestion = questions.FirstOrDefault(q => q.Id == id);
                var nextQuestion = questions
                    .Where(q => q.Urutan > currentQuestion.Urutan)
                    .OrderBy(q => q.Urutan)
                    .FirstOrDefault();

                if (nextQuestion == null)
                {
                    return (false, "Soal sudah berada di urutan terbawah");
                }

                // Swap the order
                var tempOrder = currentQuestion.Urutan;
                currentQuestion.Urutan = nextQuestion.Urutan;
                nextQuestion.Urutan = tempOrder;

                // Update both questions
                _context.PelatihanSoal.Update(currentQuestion);
                _context.PelatihanSoal.Update(nextQuestion);
                await _context.SaveChangesAsync();

                return (true, "Urutan soal berhasil diubah");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }
    }
}
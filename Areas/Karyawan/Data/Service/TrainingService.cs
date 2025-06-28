using Microsoft.EntityFrameworkCore;
using RamaExpress.Data;
using RamaExpress.Models;
using RamaExpress.Areas.Karyawan.ViewModels;
using RamaExpress.Areas.Admin.Models;
using RamaExpress.Areas.Admin.Data;

namespace RamaExpress.Areas.Karyawan.Services
{
    public class TrainingService : ITrainingService
    {
        private readonly RamaExpressAppContext _context;
        private readonly ILogger<TrainingService> _logger;

        public TrainingService(RamaExpressAppContext context, ILogger<TrainingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TrainingListViewModel> GetAvailableTrainingsAsync(int userId, int page = 1, int pageSize = 10, string search = "", string category = "")
        {
            try
            {
                var query = _context.Pelatihan
                    .Where(p => !p.IsDeleted && p.IsActive)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Judul.Contains(search) || p.Deskripsi.Contains(search));
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Kategori == category);
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var trainings = await query
                    .OrderBy(p => p.Judul)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new TrainingItemViewModel
                    {
                        Id = p.Id,
                        Judul = p.Judul,
                        Deskripsi = p.Deskripsi,
                        Kategori = p.Kategori,
                        Durasi = p.Durasi,
                        MaxPeserta = p.MaxPeserta,
                        TanggalMulai = p.TanggalMulai,
                        TanggalSelesai = p.TanggalSelesai,
                        IsEnrolled = _context.PelatihanProgress.Any(pp => pp.PelatihanId == p.Id && pp.UserId == userId),
                        CurrentParticipants = _context.PelatihanProgress.Count(pp => pp.PelatihanId == p.Id)
                    })
                    .ToListAsync();

                var categories = await _context.Pelatihan
                    .Where(p => !p.IsDeleted && p.IsActive)
                    .Select(p => p.Kategori)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return new TrainingListViewModel
                {
                    Trainings = trainings,
                    Categories = categories,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    PageSize = pageSize,
                    Search = search,
                    SelectedCategory = category
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available trainings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<TrainingDetailViewModel> GetTrainingDetailAsync(int trainingId, int userId)
        {
            try
            {
                var training = await _context.Pelatihan
                    .Where(p => p.Id == trainingId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (training == null)
                    return null;

                var isEnrolled = await _context.PelatihanProgress
                    .AnyAsync(pp => pp.PelatihanId == trainingId && pp.UserId == userId);

                var currentParticipants = await _context.PelatihanProgress
                    .CountAsync(pp => pp.PelatihanId == trainingId);

                var materials = await _context.PelatihanMateri
                    .Where(m => m.PelatihanId == trainingId && !m.IsDeleted)
                    .OrderBy(m => m.Urutan)
                    .Select(m => new TrainingMaterialViewModel
                    {
                        Id = m.Id,
                        Judul = m.Judul,
                        Deskripsi = m.Deskripsi,
                        Urutan = m.Urutan,
                        Durasi = m.Durasi
                    })
                    .ToListAsync();

                var progress = await _context.PelatihanProgress
                    .Where(pp => pp.PelatihanId == trainingId && pp.UserId == userId)
                    .FirstOrDefaultAsync();

                return new TrainingDetailViewModel
                {
                    Id = training.Id,
                    Judul = training.Judul,
                    Deskripsi = training.Deskripsi,
                    Kategori = training.Kategori,
                    Durasi = training.Durasi,
                    MaxPeserta = training.MaxPeserta,
                    TanggalMulai = training.TanggalMulai,
                    TanggalSelesai = training.TanggalSelesai,
                    Syarat = training.Syarat,
                    Benefit = training.Benefit,
                    IsEnrolled = isEnrolled,
                    CurrentParticipants = currentParticipants,
                    Materials = materials,
                    Progress = progress?.PersentaseProgress ?? 0,
                    IsCompleted = progress?.IsCompleted ?? false,
                    CanEnroll = !isEnrolled && currentParticipants < training.MaxPeserta && DateTime.Now < training.TanggalMulai
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training detail {TrainingId} for user {UserId}", trainingId, userId);
                throw;
            }
        }

        public async Task<bool> EnrollTrainingAsync(int trainingId, int userId)
        {
            try
            {
                var training = await _context.Pelatihan
                    .Where(p => p.Id == trainingId && !p.IsDeleted && p.IsActive)
                    .FirstOrDefaultAsync();

                if (training == null)
                    return false;

                var existingProgress = await _context.PelatihanProgress
                    .AnyAsync(pp => pp.PelatihanId == trainingId && pp.UserId == userId);

                if (existingProgress)
                    return false;

                var currentParticipants = await _context.PelatihanProgress
                    .CountAsync(pp => pp.PelatihanId == trainingId);

                if (currentParticipants >= training.MaxPeserta)
                    return false;

                if (DateTime.Now >= training.TanggalMulai)
                    return false;

                var progress = new PelatihanProgress
                {
                    PelatihanId = trainingId,
                    UserId = userId,
                    TanggalMulai = DateTime.Now,
                    PersentaseProgress = 0,
                    IsCompleted = false
                };

                _context.PelatihanProgress.Add(progress);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling user {UserId} to training {TrainingId}", userId, trainingId);
                return false;
            }
        }

        public async Task<MyTrainingListViewModel> GetMyTrainingsAsync(int userId, string status = "all")
        {
            try
            {
                var query = _context.PelatihanProgress
                    .Include(pp => pp.Pelatihan)
                    .Where(pp => pp.UserId == userId && !pp.Pelatihan.IsDeleted)
                    .AsQueryable();

                switch (status.ToLower())
                {
                    case "ongoing":
                        query = query.Where(pp => !pp.IsCompleted);
                        break;
                    case "completed":
                        query = query.Where(pp => pp.IsCompleted);
                        break;
                }

                var trainings = await query
                    .OrderByDescending(pp => pp.TanggalMulai)
                    .Select(pp => new MyTrainingItemViewModel
                    {
                        Id = pp.Pelatihan.Id,
                        Judul = pp.Pelatihan.Judul,
                        Kategori = pp.Pelatihan.Kategori,
                        TanggalMulai = pp.TanggalMulai,
                        TanggalSelesai = pp.CompletedAt,
                        Progress = pp.PersentaseProgress,
                        IsCompleted = pp.IsCompleted,
                        Status = pp.IsCompleted ? "Selesai" : "Berlangsung"
                    })
                    .ToListAsync();

                return new MyTrainingListViewModel
                {
                    Trainings = trainings,
                    SelectedStatus = status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my trainings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<TrainingProgressViewModel> GetTrainingProgressAsync(int trainingId, int userId)
        {
            try
            {
                var training = await _context.Pelatihan
                    .Where(p => p.Id == trainingId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (training == null)
                    return null;

                var progress = await _context.PelatihanProgress
                    .Where(pp => pp.PelatihanId == trainingId && pp.UserId == userId)
                    .FirstOrDefaultAsync();

                if (progress == null)
                    return null;

                var materials = await _context.PelatihanMateri
                    .Where(m => m.PelatihanId == trainingId && !m.IsDeleted)
                    .OrderBy(m => m.Urutan)
                    .Select(m => new TrainingMaterialProgressViewModel
                    {
                        Id = m.Id,
                        Judul = m.Judul,
                        Deskripsi = m.Deskripsi,
                        Urutan = m.Urutan,
                        Durasi = m.Durasi,
                        TipeMateri = m.TipeMateri,
                        KontenUrl = m.KontenUrl,
                        IsCompleted = _context.MateriProgress.Any(mp => mp.MateriId == m.Id && mp.UserId == userId && mp.IsCompleted)
                    })
                    .ToListAsync();

                return new TrainingProgressViewModel
                {
                    TrainingId = training.Id,
                    TrainingTitle = training.Judul,
                    OverallProgress = progress.PersentaseProgress,
                    IsCompleted = progress.IsCompleted,
                    Materials = materials,
                    CanTakeExam = progress.PersentaseProgress >= 80 && !progress.IsCompleted
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training progress {TrainingId} for user {UserId}", trainingId, userId);
                throw;
            }
        }

        public async Task<bool> UpdateProgressAsync(int trainingId, int userId, int materialId)
        {
            try
            {
                var material = await _context.PelatihanMateri
                    .Where(m => m.Id == materialId && m.PelatihanId == trainingId && !m.IsDeleted)
                    .FirstOrDefaultAsync();

                if (material == null)
                    return false;

                var existingProgress = await _context.MateriProgress
                    .Where(mp => mp.MateriId == materialId && mp.UserId == userId)
                    .FirstOrDefaultAsync();

                if (existingProgress == null)
                {
                    var materiProgress = new MateriProgress
                    {
                        MateriId = materialId,
                        UserId = userId,
                        TanggalSelesai = DateTime.Now,
                        IsCompleted = true
                    };

                    _context.MateriProgress.Add(materiProgress);
                }
                else if (!existingProgress.IsCompleted)
                {
                    existingProgress.IsCompleted = true;
                    existingProgress.TanggalSelesai = DateTime.Now;
                }

                // Update overall training progress
                var totalMaterials = await _context.PelatihanMateri
                    .CountAsync(m => m.PelatihanId == trainingId && !m.IsDeleted);

                var completedMaterials = await _context.MateriProgress
                    .Where(mp => mp.UserId == userId && _context.PelatihanMateri
                        .Any(m => m.Id == mp.MateriId && m.PelatihanId == trainingId && !m.IsDeleted && mp.IsCompleted))
                    .CountAsync();

                var overallProgress = await _context.PelatihanProgress
                    .Where(pp => pp.PelatihanId == trainingId && pp.UserId == userId)
                    .FirstOrDefaultAsync();

                if (overallProgress != null)
                {
                    overallProgress.PersentaseProgress = totalMaterials > 0 ? (int)Math.Round((double)completedMaterials / totalMaterials * 100) : 0;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for training {TrainingId}, user {UserId}, material {MaterialId}", trainingId, userId, materialId);
                return false;
            }
        }

        public async Task<bool> CompleteTrainingAsync(int trainingId, int userId)
        {
            try
            {
                var progress = await _context.PelatihanProgress
                    .Where(pp => pp.PelatihanId == trainingId && pp.UserId == userId)
                    .FirstOrDefaultAsync();

                if (progress == null || progress.IsCompleted)
                    return false;

                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.Now;
                progress.PersentaseProgress = 100;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing training {TrainingId} for user {UserId}", trainingId, userId);
                return false;
            }
        }

        public async Task<ExamViewModel> GetExamAsync(int trainingId, int userId)
        {
            try
            {
                var training = await _context.Pelatihan
                    .Where(p => p.Id == trainingId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (training == null)
                    return null;

                var progress = await _context.PelatihanProgress
                    .Where(pp => pp.PelatihanId == trainingId && pp.UserId == userId)
                    .FirstOrDefaultAsync();

                if (progress == null || progress.PersentaseProgress < 80)
                    return null;

                var questions = await _context.SoalUjian
                    .Where(s => s.PelatihanId == trainingId && !s.IsDeleted)
                    .OrderBy(s => s.Urutan)
                    .Select(s => new ExamQuestionViewModel
                    {
                        Id = s.Id,
                        Pertanyaan = s.Pertanyaan,
                        PilihanA = s.PilihanA,
                        PilihanB = s.PilihanB,
                        PilihanC = s.PilihanC,
                        PilihanD = s.PilihanD,
                        Urutan = s.Urutan
                    })
                    .ToListAsync();

                return new ExamViewModel
                {
                    TrainingId = trainingId,
                    TrainingTitle = training.Judul,
                    Questions = questions,
                    TimeLimit = 60 // 60 minutes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam for training {TrainingId}, user {UserId}", trainingId, userId);
                throw;
            }
        }

        public async Task<ExamResultViewModel> SubmitExamAsync(int trainingId, int userId, List<ExamAnswerModel> answers)
        {
            try
            {
                var questions = await _context.SoalUjian
                    .Where(s => s.PelatihanId == trainingId && !s.IsDeleted)
                    .ToListAsync();

                var correctAnswers = 0;
                var totalQuestions = questions.Count;

                foreach (var answer in answers)
                {
                    var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question != null && question.JawabanBenar == answer.SelectedAnswer)
                    {
                        correctAnswers++;
                    }
                }

                var score = totalQuestions > 0 ? (int)Math.Round((double)correctAnswers / totalQuestions * 100) : 0;
                var isPassed = score >= 70; // Minimum passing score 70%

                var examResult = new HasilUjian
                {
                    PelatihanId = trainingId,
                    UserId = userId,
                    TanggalUjian = DateTime.Now,
                    Skor = score,
                    JawabanBenar = correctAnswers,
                    TotalSoal = totalQuestions,
                    IsLulus = isPassed
                };

                _context.HasilUjian.Add(examResult);

                if (isPassed)
                {
                    // Complete training
                    await CompleteTrainingAsync(trainingId, userId);

                    // Generate certificate
                    var training = await _context.Pelatihan.FindAsync(trainingId);
                    var user = await _context.User.FindAsync(userId);

                    if (training != null && user != null)
                    {
                        var certificate = new Sertifikat
                        {
                            PelatihanId = trainingId,
                            UserId = userId,
                            NomorSertifikat = GenerateCertificateNumber(),
                            TanggalTerbit = DateTime.Now,
                            TanggalBerlaku = DateTime.Now.AddYears(2)
                        };

                        _context.Sertifikat.Add(certificate);
                    }
                }

                await _context.SaveChangesAsync();

                return new ExamResultViewModel
                {
                    Score = score,
                    CorrectAnswers = correctAnswers,
                    TotalQuestions = totalQuestions,
                    IsPassed = isPassed,
                    PassingScore = 70
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting exam for training {TrainingId}, user {UserId}", trainingId, userId);
                throw;
            }
        }

        public async Task<List<CertificateViewModel>> GetMyCertificatesAsync(int userId)
        {
            try
            {
                var certificates = await _context.Sertifikat
                    .Include(s => s.Pelatihan)
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.TanggalTerbit)
                    .Select(s => new CertificateViewModel
                    {
                        Id = s.Id,
                        NomorSertifikat = s.NomorSertifikat,
                        TrainingTitle = s.Pelatihan.Judul,
                        TanggalTerbit = s.TanggalTerbit,
                        TanggalBerlaku = s.TanggalBerlaku,
                        IsValid = s.TanggalBerlaku > DateTime.Now
                    })
                    .ToListAsync();

                return certificates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificates for user {UserId}", userId);
                throw;
            }
        }

        public async Task<byte[]> DownloadCertificateAsync(int certificateId, int userId)
        {
            try
            {
                var certificate = await _context.Sertifikat
                    .Include(s => s.Pelatihan)
                    .Include(s => s.User)
                    .Where(s => s.Id == certificateId && s.UserId == userId)
                    .FirstOrDefaultAsync();

                if (certificate == null)
                    return null;

                // Generate PDF certificate (implement PDF generation logic here)
                // For now, return placeholder
                return new byte[0];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate {CertificateId} for user {UserId}", certificateId, userId);
                throw;
            }
        }

        private string GenerateCertificateNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var random = new Random().Next(1000, 9999);
            return $"RE-{year}{month:D2}-{random}";
        }
    }
}
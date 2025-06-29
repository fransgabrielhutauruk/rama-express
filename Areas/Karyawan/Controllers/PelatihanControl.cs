using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using RamaExpress.Areas.Karyawan.Models;

namespace RamaExpress.Areas.Karyawan.Controllers
{
    [Area("Karyawan")]
    public class PelatihanController : Controller
    {
        private readonly RamaExpressAppContext _context;
        private readonly ILogger<PelatihanController> _logger;

        public PelatihanController(RamaExpressAppContext context, ILogger<PelatihanController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Route("Karyawan/Pelatihan")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                // Get user's position for filtering available trainings
                var user = await _context.User.FindAsync(userId.Value);
                if (user == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                // Get available trainings for user's position
                var availableTrainings = await GetAvailableTrainings(user.Posisi);

                // Get user's training progress
                var userProgress = await GetUserTrainingProgress(userId.Value);

                // Get completed trainings with results
                var completedTrainings = await GetCompletedTrainings(userId.Value);

                var viewModel = new KaryawanPelatihanViewModel
                {
                    AvailableTrainings = availableTrainings,
                    OngoingTrainings = userProgress,
                    CompletedTrainings = completedTrainings,
                    TotalAvailable = availableTrainings.Count(),
                    TotalOngoing = userProgress.Count(),
                    TotalCompleted = completedTrainings.Count()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading training index for user {UserId}", HttpContext.Session.GetInt32("UserId"));
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data pelatihan.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Route("Karyawan/Pelatihan/Detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var pelatihan = await _context.Pelatihan
                    .Include(p => p.PelatihanMateris.OrderBy(m => m.Urutan))
                    .Include(p => p.PelatihanSoals.OrderBy(s => s.Urutan))
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.IsActive);

                if (pelatihan == null)
                {
                    TempData["ErrorMessage"] = "Pelatihan tidak ditemukan atau tidak aktif.";
                    return RedirectToAction("Index");
                }

                // Check if user has access to this training
                var user = await _context.User.FindAsync(userId.Value);
                var hasAccess = await HasAccessToTraining(user.Posisi, id);

                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "Anda tidak memiliki akses ke pelatihan ini.";
                    return RedirectToAction("Index");
                }

                // Get user's progress for this training
                var progress = await _context.PelatihanProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.PelatihanId == id);

                // Get training result if completed
                var hasil = await _context.PelatihanHasil
                    .FirstOrDefaultAsync(h => h.UserId == userId.Value && h.PelatihanId == id);

                // Get certificate if exists
                var sertifikat = await _context.Sertifikat
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.PelatihanId == id);

                var viewModel = new PelatihanDetailViewModel
                {
                    Pelatihan = pelatihan,
                    Progress = progress,
                    Hasil = hasil,
                    Sertifikat = sertifikat,
                    CanStart = progress == null,
                    CanContinue = progress != null && !progress.IsCompleted,
                    CanTakeExam = progress != null && progress.IsCompleted && hasil == null,
                    IsCompleted = hasil != null
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading training detail {TrainingId} for user {UserId}", id, HttpContext.Session.GetInt32("UserId"));
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat detail pelatihan.";
                return RedirectToAction("Index");
            }
        }

        [Route("Karyawan/Pelatihan/Mulai/{id}")]
        public async Task<IActionResult> Mulai(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                // Check if training exists and is active
                var pelatihan = await _context.Pelatihan
                    .Include(p => p.PelatihanMateris.OrderBy(m => m.Urutan))
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.IsActive);

                if (pelatihan == null)
                {
                    TempData["ErrorMessage"] = "Pelatihan tidak ditemukan atau tidak aktif.";
                    return RedirectToAction("Index");
                }

                // Check access
                var user = await _context.User.FindAsync(userId.Value);
                var hasAccess = await HasAccessToTraining(user.Posisi, id);

                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "Anda tidak memiliki akses ke pelatihan ini.";
                    return RedirectToAction("Index");
                }

                // Check if already started
                var existingProgress = await _context.PelatihanProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.PelatihanId == id);

                if (existingProgress != null)
                {
                    return RedirectToAction("Materi", new { id = id, materiId = existingProgress.MateriTerakhirId });
                }

                // Create new progress record
                var progress = new PelatihanProgress
                {
                    UserId = userId.Value,
                    PelatihanId = id,
                    MateriTerakhirId = 1,
                    IsCompleted = false,
                    StartedAt = DateTime.Now
                };

                _context.PelatihanProgress.Add(progress);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Pelatihan berhasil dimulai!";

                // Redirect to first material
                var firstMaterial = pelatihan.PelatihanMateris.OrderBy(m => m.Urutan).FirstOrDefault();
                if (firstMaterial != null)
                {
                    return RedirectToAction("Materi", new { id = id, materiId = firstMaterial.Id });
                }

                return RedirectToAction("Detail", new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting training {TrainingId} for user {UserId}", id, HttpContext.Session.GetInt32("UserId"));
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memulai pelatihan.";
                return RedirectToAction("Detail", new { id = id });
            }
        }

        [Route("Karyawan/Pelatihan/Materi/{id}/{materiId}")]
        public async Task<IActionResult> Materi(int id, int materiId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                // Get training and material
                var pelatihan = await _context.Pelatihan
                    .Include(p => p.PelatihanMateris.OrderBy(m => m.Urutan))
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.IsActive);

                if (pelatihan == null)
                {
                    TempData["ErrorMessage"] = "Pelatihan tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                var materi = pelatihan.PelatihanMateris.FirstOrDefault(m => m.Id == materiId);
                if (materi == null)
                {
                    TempData["ErrorMessage"] = "Materi tidak ditemukan.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Check progress
                var progress = await _context.PelatihanProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.PelatihanId == id);

                if (progress == null)
                {
                    TempData["ErrorMessage"] = "Anda belum memulai pelatihan ini.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Update progress
                progress.MateriTerakhirId = materi.Urutan;
                progress.UpdatedAt = DateTime.Now;

                // Check if this is the last material
                var totalMaterials = pelatihan.PelatihanMateris.Count();
                if (materi.Urutan >= totalMaterials)
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                var viewModel = new MateriViewModel
                {
                    Pelatihan = pelatihan,
                    CurrentMateri = materi,
                    Progress = progress,
                    AllMaterials = pelatihan.PelatihanMateris.ToList(),
                    NextMaterial = pelatihan.PelatihanMateris.FirstOrDefault(m => m.Urutan > materi.Urutan),
                    PreviousMaterial = pelatihan.PelatihanMateris.FirstOrDefault(m => m.Urutan < materi.Urutan),
                    IsLastMaterial = materi.Urutan >= totalMaterials
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading material {MaterialId} for training {TrainingId}", materiId, id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat materi.";
                return RedirectToAction("Detail", new { id = id });
            }
        }

        [Route("Karyawan/Pelatihan/Ujian/{id}")]
        public async Task<IActionResult> Ujian(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                // Check if training is completed
                var progress = await _context.PelatihanProgress
                    .Include(p => p.Pelatihan)
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.PelatihanId == id);

                if (progress == null || !progress.IsCompleted)
                {
                    TempData["ErrorMessage"] = "Anda harus menyelesaikan semua materi terlebih dahulu.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Check if already taken exam
                var existingResult = await _context.PelatihanHasil
                    .FirstOrDefaultAsync(h => h.UserId == userId.Value && h.PelatihanId == id);

                if (existingResult != null)
                {
                    TempData["InfoMessage"] = "Anda sudah menyelesaikan ujian untuk pelatihan ini.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Get questions
                var questions = await _context.PelatihanSoal
                    .Where(s => s.PelatihanId == id)
                    .OrderBy(s => s.Urutan)
                    .ToListAsync();

                if (!questions.Any())
                {
                    TempData["ErrorMessage"] = "Ujian belum tersedia untuk pelatihan ini.";
                    return RedirectToAction("Detail", new { id = id });
                }

                var viewModel = new UjianViewModel
                {
                    Pelatihan = progress.Pelatihan,
                    Questions = questions,
                    TimeLimit = progress.Pelatihan.DurasiMenit * 60, // Convert to seconds
                    MinScore = progress.Pelatihan.SkorMinimal
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading exam for training {TrainingId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat ujian.";
                return RedirectToAction("Detail", new { id = id });
            }
        }

        [Route("Karyawan/Pelatihan/SubmitUjian/{id}")]
        [HttpPost]
        public async Task<IActionResult> SubmitUjian(int id, Dictionary<int, string> answers)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Sesi telah berakhir" });
                }

                // Get training and questions
                var pelatihan = await _context.Pelatihan.FindAsync(id);
                var questions = await _context.PelatihanSoal
                    .Where(s => s.PelatihanId == id)
                    .ToListAsync();

                if (pelatihan == null || !questions.Any())
                {
                    return Json(new { success = false, message = "Data tidak valid" });
                }

                // Calculate score
                int correctAnswers = 0;
                foreach (var question in questions)
                {
                    if (answers.TryGetValue(question.Id, out string userAnswer))
                    {
                        if (userAnswer?.ToUpper() == question.JawabanBenar.ToUpper())
                        {
                            correctAnswers++;
                        }
                    }
                }

                var score = (int)Math.Round((double)correctAnswers / questions.Count * 100);
                var isPass = score >= pelatihan.SkorMinimal;

                // Save result
                var hasil = new PelatihanHasil
                {
                    UserId = userId.Value,
                    PelatihanId = id,
                    Skor = score,
                    IsLulus = isPass,
                    TanggalSelesai = DateTime.Now
                };

                _context.PelatihanHasil.Add(hasil);

                // Generate certificate if passed
                if (isPass)
                {
                    await GenerateCertificate(userId.Value, id);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    score = score,
                    isPass = isPass,
                    minScore = pelatihan.SkorMinimal,
                    redirectUrl = Url.Action("HasilUjian", new { id = id })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting exam for training {TrainingId}", id);
                return Json(new { success = false, message = "Terjadi kesalahan saat menyimpan hasil ujian" });
            }
        }

        [Route("Karyawan/Pelatihan/HasilUjian/{id}")]
        public async Task<IActionResult> HasilUjian(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var hasil = await _context.PelatihanHasil
                    .Include(h => h.Pelatihan)
                    .Include(h => h.User)
                    .FirstOrDefaultAsync(h => h.UserId == userId.Value && h.PelatihanId == id);

                if (hasil == null)
                {
                    TempData["ErrorMessage"] = "Hasil ujian tidak ditemukan.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Get certificate if exists
                var sertifikat = await _context.Sertifikat
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.PelatihanId == id);

                var viewModel = new HasilUjianViewModel
                {
                    Hasil = hasil,
                    Sertifikat = sertifikat
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading exam result for training {TrainingId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat hasil ujian.";
                return RedirectToAction("Detail", new { id = id });
            }
        }

        private async Task<IEnumerable<Pelatihan>> GetAvailableTrainings(string userPosition)
        {
            // Get position ID
            var posisi = await _context.Posisi
                .FirstOrDefaultAsync(p => p.Name == userPosition && !p.IsDeleted);

            if (posisi == null)
            {
                return new List<Pelatihan>();
            }

            // Get trainings for this position
            var pelatihanIds = await _context.PelatihanPosisi
                .Where(pp => pp.PosisiId == posisi.Id)
                .Select(pp => pp.PelatihanId)
                .ToListAsync();

            return await _context.Pelatihan
                .Where(p => pelatihanIds.Contains(p.Id) && !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.Judul)
                .ToListAsync();
        }

        private async Task<IEnumerable<PelatihanProgress>> GetUserTrainingProgress(int userId)
        {
            return await _context.PelatihanProgress
                .Include(p => p.Pelatihan)
                .Where(p => p.UserId == userId && !p.IsCompleted)
                .OrderByDescending(p => p.UpdatedAt ?? p.StartedAt)
                .ToListAsync();
        }

        private async Task<IEnumerable<PelatihanHasil>> GetCompletedTrainings(int userId)
        {
            return await _context.PelatihanHasil
                .Include(h => h.Pelatihan)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.TanggalSelesai)
                .ToListAsync();
        }

        private async Task<bool> HasAccessToTraining(string userPosition, int trainingId)
        {
            var posisi = await _context.Posisi
                .FirstOrDefaultAsync(p => p.Name == userPosition && !p.IsDeleted);

            if (posisi == null) return false;

            return await _context.PelatihanPosisi
                .AnyAsync(pp => pp.PosisiId == posisi.Id && pp.PelatihanId == trainingId);
        }

        private async Task GenerateCertificate(int userId, int pelatihanId)
        {
            try
            {
                // Check if certificate settings exist
                var certificateSettings = await _context.PelatihanSertifikat
                    .FirstOrDefaultAsync(ps => ps.PelatihanId == pelatihanId && ps.IsSertifikatActive);

                if (certificateSettings == null) return;

                // Generate certificate number
                var now = DateTime.Now;
                var count = await _context.Sertifikat.CountAsync(s => s.PelatihanId == pelatihanId);
                var certificateNumber = certificateSettings.CertificateNumberFormat
                    .Replace("{YEAR}", now.ToString("yyyy"))
                    .Replace("{MONTH}", now.ToString("MM"))
                    .Replace("{DAY}", now.ToString("dd"))
                    .Replace("{PELATIHAN_ID}", pelatihanId.ToString("D3"))
                    .Replace("{INCREMENT}", (count + 1).ToString("D4"));

                // Calculate expiration date
                var expirationDate = certificateSettings.CalculateExpirationDate(now) ?? DateTime.MaxValue;

                var sertifikat = new Sertifikat
                {
                    UserId = userId,
                    PelatihanId = pelatihanId,
                    NomorSertifikat = certificateNumber,
                    TanggalTerbit = now,
                    TanggalKadaluarsa = expirationDate
                };

                _context.Sertifikat.Add(sertifikat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate for user {UserId} training {TrainingId}", userId, pelatihanId);
            }
        }
    }
}
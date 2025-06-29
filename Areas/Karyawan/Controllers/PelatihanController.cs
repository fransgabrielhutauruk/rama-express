// Areas/Karyawan/Controllers/PelatihanController.cs - CORRECTED VERSION
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

        // ===== 3. FIX MULAI METHOD =====
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
                    // Find current material based on MateriTerakhirId
                    var currentMaterial = pelatihan.PelatihanMateris
                        .FirstOrDefault(m => m.Id == existingProgress.MateriTerakhirId);

                    if (currentMaterial != null)
                    {
                        return RedirectToAction("Materi", new { id = id, materiId = currentMaterial.Id });
                    }

                    return RedirectToAction("Detail", new { id = id });
                }

                // Get first material
                var firstMaterial = pelatihan.PelatihanMateris.OrderBy(m => m.Urutan).FirstOrDefault();
                if (firstMaterial == null)
                {
                    TempData["ErrorMessage"] = "Pelatihan tidak memiliki materi yang tersedia.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // 🔧 FIXED: Create progress with first material ID
                var progress = new PelatihanProgress
                {
                    UserId = userId.Value,
                    PelatihanId = id,
                    MateriTerakhirId = firstMaterial.Id, // Use actual material ID, not Urutan
                    IsCompleted = false,
                    StartedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.PelatihanProgress.Add(progress);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Pelatihan berhasil dimulai!";
                return RedirectToAction("Materi", new { id = id, materiId = firstMaterial.Id });
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

                var pelatihan = await _context.Pelatihan
                    .Include(p => p.PelatihanMateris.OrderBy(m => m.Urutan))
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.IsActive);

                if (pelatihan == null)
                {
                    TempData["ErrorMessage"] = "Pelatihan tidak ditemukan atau tidak aktif.";
                    return RedirectToAction("Index");
                }

                var currentMateri = pelatihan.PelatihanMateris.FirstOrDefault(m => m.Id == materiId);
                if (currentMateri == null)
                {
                    TempData["ErrorMessage"] = "Materi tidak ditemukan.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Get user's progress
                var progress = await _context.PelatihanProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.PelatihanId == id);

                if (progress == null)
                {
                    TempData["ErrorMessage"] = "Progress pelatihan tidak ditemukan. Silakan mulai pelatihan terlebih dahulu.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Get all materials for navigation
                var allMaterials = pelatihan.PelatihanMateris.OrderBy(m => m.Urutan).ToList();
                var currentIndex = allMaterials.FindIndex(m => m.Id == materiId);

                var viewModel = new MateriViewModel
                {
                    Pelatihan = pelatihan,
                    CurrentMateri = currentMateri,
                    Progress = progress,
                    AllMaterials = allMaterials,
                    NextMaterial = currentIndex < allMaterials.Count - 1 ? allMaterials[currentIndex + 1] : null,
                    PreviousMaterial = currentIndex > 0 ? allMaterials[currentIndex - 1] : null,
                    IsLastMaterial = currentIndex == allMaterials.Count - 1
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

        [Route("Karyawan/Pelatihan/CompleteMateri/{id}/{materiId}")]
        [HttpPost]
        public async Task<IActionResult> CompleteMateri(int id, int materiId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var progress = await _context.PelatihanProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.PelatihanId == id);

                if (progress == null)
                {
                    return Json(new { success = false, message = "Progress tidak ditemukan" });
                }

                // 🔧 FIXED: Get pelatihan with materials ordered by Urutan
                var pelatihan = await _context.Pelatihan
                    .Include(p => p.PelatihanMateris.OrderBy(m => m.Urutan))
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pelatihan == null)
                {
                    return Json(new { success = false, message = "Pelatihan tidak ditemukan" });
                }

                var allMaterials = pelatihan.PelatihanMateris.OrderBy(m => m.Urutan).ToList();
                var currentMaterial = allMaterials.FirstOrDefault(m => m.Id == materiId);

                if (currentMaterial == null)
                {
                    return Json(new { success = false, message = "Materi tidak ditemukan" });
                }

                var currentIndex = allMaterials.FindIndex(m => m.Id == materiId);

                // 🔧 FIXED: Update progress logic
                if (currentIndex < allMaterials.Count - 1)
                {
                    // Not the last material, move to next
                    var nextMaterial = allMaterials[currentIndex + 1];
                    progress.MateriTerakhirId = nextMaterial.Id;
                    progress.IsCompleted = false; // Make sure it's not completed yet
                }
                else
                {
                    // 🔧 CRITICAL FIX: This is the last material, mark as completed
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.Now;
                    // Keep MateriTerakhirId as the last material
                }

                progress.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // 🔧 FIXED: Return proper response
                if (progress.IsCompleted)
                {
                    return Json(new
                    {
                        success = true,
                        isCompleted = true,
                        message = "🎉 Semua materi telah selesai! Anda dapat mengikuti ujian sekarang.",
                        redirectUrl = Url.Action("Detail", new { id = id })
                    });
                }
                else
                {
                    var nextMaterial = allMaterials[currentIndex + 1];
                    return Json(new
                    {
                        success = true,
                        isCompleted = false,
                        message = "✅ Materi berhasil diselesaikan! Lanjut ke materi berikutnya.",
                        redirectUrl = Url.Action("Materi", new { id = id, materiId = nextMaterial.Id })
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing material {MaterialId} for training {TrainingId}", materiId, id);
                return Json(new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        // ===== 2. FIX UJIAN ACCESS CHECK =====
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

                var pelatihan = await _context.Pelatihan
                    .Include(p => p.PelatihanSoals.OrderBy(s => s.Urutan))
                    .Include(p => p.PelatihanMateris) // Include materials for checking
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.IsActive);

                if (pelatihan == null)
                {
                    TempData["ErrorMessage"] = "Pelatihan tidak ditemukan atau tidak aktif.";
                    return RedirectToAction("Index");
                }

                // 🔧 CRITICAL FIX: Check if user has completed all materials
                var progress = await _context.PelatihanProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.PelatihanId == id);

                // 🔧 FIXED: Better completion check logic
                if (progress == null)
                {
                    TempData["ErrorMessage"] = "Anda belum memulai pelatihan ini.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // 🔧 CRITICAL FIX: Check IsCompleted flag
                if (!progress.IsCompleted)
                {
                    // Additional check: verify if all materials are actually completed
                    var totalMaterials = pelatihan.PelatihanMateris?.Count() ?? 0;
                    var currentMaterialIndex = pelatihan.PelatihanMateris?
                        .OrderBy(m => m.Urutan)
                        .ToList()
                        .FindIndex(m => m.Id == progress.MateriTerakhirId) ?? -1;

                    // Debug info
                    TempData["ErrorMessage"] = $"Anda harus menyelesaikan semua materi terlebih dahulu. " +
                        $"Progress: {currentMaterialIndex + 1}/{totalMaterials}, IsCompleted: {progress.IsCompleted}";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Check if already taken exam
                var existingResult = await _context.PelatihanHasil
                    .FirstOrDefaultAsync(h => h.UserId == userId.Value && h.PelatihanId == id);

                if (existingResult != null)
                {
                    TempData["ErrorMessage"] = "Anda sudah mengikuti ujian untuk pelatihan ini.";
                    return RedirectToAction("HasilUjian", new { id = id });
                }

                if (!pelatihan.PelatihanSoals.Any())
                {
                    TempData["ErrorMessage"] = "Ujian tidak tersedia untuk pelatihan ini.";
                    return RedirectToAction("Detail", new { id = id });
                }

                var viewModel = new UjianViewModel
                {
                    Pelatihan = pelatihan,
                    Questions = pelatihan.PelatihanSoals.OrderBy(s => s.Urutan).ToList(),
                    TimeLimit = pelatihan.DurasiMenit * 60, // Convert to seconds
                    MinScore = pelatihan.SkorMinimal
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
        public async Task<IActionResult> SubmitUjian(int id, List<string> answers)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var pelatihan = await _context.Pelatihan
                    .Include(p => p.PelatihanSoals.OrderBy(s => s.Urutan))
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pelatihan == null)
                {
                    return Json(new { success = false, message = "Pelatihan tidak ditemukan" });
                }

                // Check if already submitted
                var existingResult = await _context.PelatihanHasil
                    .FirstOrDefaultAsync(h => h.UserId == userId.Value && h.PelatihanId == id);

                if (existingResult != null)
                {
                    return Json(new { success = false, message = "Ujian sudah pernah dikerjakan" });
                }

                // Calculate score
                var questions = pelatihan.PelatihanSoals.OrderBy(s => s.Urutan).ToList();
                var correctAnswers = 0;

                for (int i = 0; i < Math.Min(questions.Count, answers.Count); i++)
                {
                    if (questions[i].JawabanBenar.Equals(answers[i], StringComparison.OrdinalIgnoreCase))
                    {
                        correctAnswers++;
                    }
                }

                var score = questions.Count > 0 ? (int)Math.Round((double)correctAnswers / questions.Count * 100) : 0;
                var isLulus = score >= pelatihan.SkorMinimal;

                // Save result - CORRECTED: Use TanggalSelesai instead of TanggalUjian
                var hasil = new PelatihanHasil
                {
                    UserId = userId.Value,
                    PelatihanId = id,
                    TanggalSelesai = DateTime.Now, // CORRECTED
                    Skor = score,
                    IsLulus = isLulus
                    // REMOVED: JawabanBenar and TotalSoal are not in the actual model
                };

                _context.PelatihanHasil.Add(hasil);

                // Generate certificate if passed
                if (isLulus)
                {
                    await GenerateCertificate(userId.Value, id);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    score = score,
                    isLulus = isLulus,
                    redirectUrl = Url.Action("HasilUjian", new { id = id })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting exam for training {TrainingId}", id);
                return Json(new { success = false, message = "Terjadi kesalahan sistem" });
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

        // HELPER METHODS
        private async Task<IEnumerable<Pelatihan>> GetAvailableTrainings(string userPosition)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available trainings for position {Position}", userPosition);
                return new List<Pelatihan>();
            }
        }

        private async Task<IEnumerable<PelatihanProgress>> GetUserTrainingProgress(int userId)
        {
            try
            {
                return await _context.PelatihanProgress
                    .Include(p => p.Pelatihan)
                    .Where(p => p.UserId == userId && !p.IsCompleted)
                    .OrderByDescending(p => p.UpdatedAt ?? p.StartedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training progress for user {UserId}", userId);
                return new List<PelatihanProgress>();
            }
        }

        private async Task<IEnumerable<PelatihanHasil>> GetCompletedTrainings(int userId)
        {
            try
            {
                return await _context.PelatihanHasil
                    .Include(h => h.Pelatihan)
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.TanggalSelesai) // CORRECTED
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed trainings for user {UserId}", userId);
                return new List<PelatihanHasil>();
            }
        }

        private async Task<bool> HasAccessToTraining(string userPosition, int pelatihanId)
        {
            try
            {
                var posisi = await _context.Posisi
                    .FirstOrDefaultAsync(p => p.Name == userPosition && !p.IsDeleted);

                if (posisi == null) return false;

                return await _context.PelatihanPosisi
                    .AnyAsync(pp => pp.PosisiId == posisi.Id && pp.PelatihanId == pelatihanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking access to training {TrainingId} for position {Position}", pelatihanId, userPosition);
                return false;
            }
        }

        private async Task GenerateCertificate(int userId, int pelatihanId)
        {
            try
            {
                var user = await _context.User.FindAsync(userId);
                var pelatihan = await _context.Pelatihan.FindAsync(pelatihanId);

                if (user == null || pelatihan == null) return;

                // Check if certificate config exists
                var sertifikatConfig = await _context.PelatihanSertifikat
                    .FirstOrDefaultAsync(ps => ps.PelatihanId == pelatihanId && ps.IsSertifikatActive);

                if (sertifikatConfig == null) return;

                // Generate certificate number
                var now = DateTime.Now;
                var existingCount = await _context.Sertifikat
                    .CountAsync(s => s.TanggalTerbit.Year == now.Year && s.TanggalTerbit.Month == now.Month);

                var nomorSertifikat = sertifikatConfig.CertificateNumberFormat
                    .Replace("{YEAR}", now.Year.ToString())
                    .Replace("{MONTH}", now.Month.ToString("D2"))
                    .Replace("{INCREMENT}", (existingCount + 1).ToString("D4"));

                // CORRECTED: Use TanggalKadaluarsa instead of TanggalBerlaku
                var sertifikat = new Sertifikat
                {
                    UserId = userId,
                    PelatihanId = pelatihanId,
                    NomorSertifikat = nomorSertifikat,
                    TanggalTerbit = now,
                    TanggalKadaluarsa = sertifikatConfig.CalculateExpirationDate(now) ?? DateTime.MaxValue // CORRECTED
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
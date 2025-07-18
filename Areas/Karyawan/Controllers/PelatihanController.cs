// Areas/Karyawan/Controllers/PelatihanController.cs - CORRECTED VERSION
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using RamaExpress.Areas.Karyawan.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;

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

        protected virtual int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        [Route("Karyawan")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId(); 
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
                _logger.LogError(ex, "Error loading training index for user {UserId}", GetCurrentUserId()); // ✅ Changed
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data pelatihan.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Route("Karyawan/Pelatihan/Detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var userId = GetCurrentUserId(); 
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
                _logger.LogError(ex, "Error loading training detail {TrainingId} for user {UserId}", id, GetCurrentUserId()); // ✅ Changed
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat detail pelatihan.";
                return RedirectToAction("Index");
            }
        }

        [Route("Karyawan/Pelatihan/Mulai/{id}")]
        public async Task<IActionResult> Mulai(int id)
        {
            try
            {
                var userId = GetCurrentUserId(); 
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

                // Create progress with first material ID
                var progress = new PelatihanProgress
                {
                    UserId = userId.Value,
                    PelatihanId = id,
                    MateriTerakhirId = firstMaterial.Id,
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
                _logger.LogError(ex, "Error starting training {TrainingId} for user {UserId}", id, GetCurrentUserId()); // ✅ Changed
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memulai pelatihan.";
                return RedirectToAction("Detail", new { id = id });
            }
        }

        [Route("Karyawan/Pelatihan/Materi/{id}/{materiId}")]
        public async Task<IActionResult> Materi(int id, int materiId)
        {
            try
            {
                var userId = GetCurrentUserId(); 
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

                // Sequential access control
                var allMaterials = pelatihan.PelatihanMateris.OrderBy(m => m.Urutan).ToList();
                var requestedMaterialIndex = allMaterials.FindIndex(m => m.Id == materiId);
                var currentProgressIndex = allMaterials.FindIndex(m => m.Id == progress.MateriTerakhirId);

                // Prevent jumping ahead to future materials
                if (requestedMaterialIndex > currentProgressIndex + 1)
                {
                    var currentMaterial = allMaterials.FirstOrDefault(m => m.Id == progress.MateriTerakhirId);
                    TempData["ErrorMessage"] = $"Anda harus menyelesaikan materi secara berurutan. " +
                        $"Silakan lanjutkan dari materi: {currentMaterial?.Judul}";
                    return RedirectToAction("Materi", new { id = id, materiId = progress.MateriTerakhirId });
                }

                // Auto-update progress when accessing new material
                if (requestedMaterialIndex == currentProgressIndex + 1)
                {
                    // User is accessing the next material in sequence
                    progress.MateriTerakhirId = materiId;
                    progress.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Prepare view model
                var currentIndex = allMaterials.FindIndex(m => m.Id == materiId);
                var viewModel = new MateriViewModel
                {
                    Pelatihan = pelatihan,
                    CurrentMateri = currentMateri,
                    Progress = progress,
                    AllMaterials = allMaterials,
                    NextMaterial = currentIndex < allMaterials.Count - 1 ?
                        allMaterials[currentIndex + 1] : null,
                    PreviousMaterial = currentIndex > 0 ?
                        allMaterials[currentIndex - 1] : null,
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
                var userId = GetCurrentUserId();
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

                // Proper completion logic
                if (currentIndex < allMaterials.Count - 1)
                {
                    // Not the last material, move to next
                    var nextMaterial = allMaterials[currentIndex + 1];
                    progress.MateriTerakhirId = nextMaterial.Id;
                    progress.IsCompleted = false;
                }
                else
                {
                    // This is the last material - mark as completed
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.Now;
                    progress.MateriTerakhirId = currentMaterial.Id;
                }

                progress.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Return proper response based on completion status
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

        [Route("Karyawan/Pelatihan/Ujian/{id}")]
        public async Task<IActionResult> Ujian(int id)
        {
            try
            {
                var userId = GetCurrentUserId(); 
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var pelatihan = await _context.Pelatihan
                    .Include(p => p.PelatihanSoals.OrderBy(s => s.Urutan))
                    .Include(p => p.PelatihanMateris.OrderBy(m => m.Urutan))
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.IsActive);

                if (pelatihan == null)
                {
                    TempData["ErrorMessage"] = "Pelatihan tidak ditemukan atau tidak aktif.";
                    return RedirectToAction("Index");
                }

                // Check if user has completed all materials
                var progress = await _context.PelatihanProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.PelatihanId == id);

                if (progress == null)
                {
                    TempData["ErrorMessage"] = "Anda belum memulai pelatihan ini.";
                    return RedirectToAction("Detail", new { id = id });
                }

                // Primary check using IsCompleted flag
                if (!progress.IsCompleted)
                {
                    var totalMaterials = pelatihan.PelatihanMateris?.Count() ?? 0;

                    if (totalMaterials == 0)
                    {
                        progress.IsCompleted = true;
                        progress.CompletedAt = DateTime.Now;
                        progress.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var materialsOrdered = pelatihan.PelatihanMateris.OrderBy(m => m.Urutan).ToList();
                        var lastMaterial = materialsOrdered.LastOrDefault();
                        var currentMaterialIndex = materialsOrdered.FindIndex(m => m.Id == progress.MateriTerakhirId);

                        if (lastMaterial != null && progress.MateriTerakhirId == lastMaterial.Id)
                        {
                            progress.IsCompleted = true;
                            progress.CompletedAt = DateTime.Now;
                            progress.UpdatedAt = DateTime.Now;
                            await _context.SaveChangesAsync();

                            TempData["SuccessMessage"] = "Progress Anda telah diperbarui. Sekarang Anda dapat mengikuti ujian.";
                        }
                        else
                        {
                            var completedCount = Math.Max(0, currentMaterialIndex + 1);
                            TempData["ErrorMessage"] = $"Anda harus menyelesaikan semua materi terlebih dahulu. " +
                                $"Progress: {completedCount}/{totalMaterials} materi selesai.";
                            return RedirectToAction("Detail", new { id = id });
                        }
                    }
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
                    TimeLimit = 120 * 60,
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
                var userId = GetCurrentUserId(); 
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

                // Save result
                var hasil = new PelatihanHasil
                {
                    UserId = userId.Value,
                    PelatihanId = id,
                    TanggalSelesai = DateTime.Now,
                    Skor = score,
                    IsLulus = isLulus
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
                var userId = GetCurrentUserId(); 
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
                var posisi = await _context.Posisi
                    .FirstOrDefaultAsync(p => p.Name == userPosition && !p.IsDeleted);

                if (posisi == null)
                {
                    return new List<Pelatihan>();
                }

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
                    .OrderByDescending(h => h.TanggalSelesai)
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

                var sertifikatConfig = await _context.PelatihanSertifikat
                    .FirstOrDefaultAsync(ps => ps.PelatihanId == pelatihanId && ps.IsSertifikatActive);

                if (sertifikatConfig == null) return;

                var now = DateTime.Now;
                var existingCount = await _context.Sertifikat
                    .CountAsync(s => s.TanggalTerbit.Year == now.Year && s.TanggalTerbit.Month == now.Month);

                var nomorSertifikat = sertifikatConfig.CertificateNumberFormat
                    .Replace("{YEAR}", now.Year.ToString())
                    .Replace("{MONTH}", now.Month.ToString("D2"))
                    .Replace("{INCREMENT}", (existingCount + 1).ToString("D4"));

                var sertifikat = new Sertifikat
                {
                    UserId = userId,
                    PelatihanId = pelatihanId,
                    NomorSertifikat = nomorSertifikat,
                    TanggalTerbit = now,
                    TanggalKadaluarsa = sertifikatConfig.CalculateExpirationDate(now) ?? DateTime.MaxValue
                };

                _context.Sertifikat.Add(sertifikat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate for user {UserId} training {TrainingId}", userId, pelatihanId);
            }
        }

        [Route("Karyawan/Pelatihan/Sertifikat")]
        public async Task<IActionResult> Sertifikat()
        {
            try
            {
                var userId = GetCurrentUserId(); 
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var sertifikats = await _context.Sertifikat
                    .Include(s => s.User)
                    .Include(s => s.Pelatihan)
                    .Where(s => s.UserId == userId.Value)
                    .OrderByDescending(s => s.TanggalTerbit)
                    .ToListAsync();

                ViewBag.TotalSertifikat = sertifikats.Count;
                ViewBag.SertifikatAktif = sertifikats.Count(s => s.TanggalKadaluarsa == DateTime.MaxValue || s.TanggalKadaluarsa > DateTime.Now);
                ViewBag.SertifikatKadaluarsa = sertifikats.Count(s => s.TanggalKadaluarsa != DateTime.MaxValue && s.TanggalKadaluarsa <= DateTime.Now);

                return View(sertifikats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates for user {UserId}", GetCurrentUserId()); // ✅ Changed
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data sertifikat.";
                return View(new List<Sertifikat>());
            }
        }

        [Route("Karyawan/Pelatihan/SertifikatDetail/{id}")]
        public async Task<IActionResult> SertifikatDetail(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var sertifikat = await _context.Sertifikat
                    .Include(s => s.User)
                    .Include(s => s.Pelatihan)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId.Value);

                if (sertifikat == null)
                {
                    TempData["ErrorMessage"] = "Sertifikat tidak ditemukan atau bukan milik Anda.";
                    return RedirectToAction(nameof(Sertifikat));
                }

                ViewBag.IsExpired = sertifikat.TanggalKadaluarsa != DateTime.MaxValue && sertifikat.TanggalKadaluarsa < DateTime.Now;
                ViewBag.DaysUntilExpiry = sertifikat.TanggalKadaluarsa != DateTime.MaxValue ?
                    (int)(sertifikat.TanggalKadaluarsa - DateTime.Now).TotalDays : -1;

                return View(sertifikat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate detail {CertificateId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat detail sertifikat.";
                return RedirectToAction(nameof(Sertifikat));
            }
        }

        [Route("Karyawan/Pelatihan/DownloadSertifikat/{id}")]
        public async Task<IActionResult> DownloadSertifikat(int id)
        {
            try
            {
                var userId = GetCurrentUserId(); 
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var sertifikat = await _context.Sertifikat
                    .Include(s => s.User)
                    .Include(s => s.Pelatihan)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId.Value);

                if (sertifikat == null)
                {
                    TempData["ErrorMessage"] = "Sertifikat tidak ditemukan atau bukan milik Anda.";
                    return RedirectToAction(nameof(Sertifikat));
                }

                var pdfBytes = await GenerateSertifikatPdf(sertifikat);
                var fileName = $"Sertifikat_{sertifikat.NomorSertifikat.Replace("/", "_")}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate {CertificateId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mendownload sertifikat.";
                return RedirectToAction(nameof(Sertifikat));
            }
        }

        [Route("Karyawan/Pelatihan/PreviewSertifikat/{id}")]
        public async Task<IActionResult> PreviewSertifikat(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var sertifikat = await _context.Sertifikat
                    .Include(s => s.User)
                    .Include(s => s.Pelatihan)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId.Value);

                if (sertifikat == null)
                {
                    TempData["ErrorMessage"] = "Sertifikat tidak ditemukan atau bukan milik Anda.";
                    return RedirectToAction(nameof(Sertifikat));
                }

                var pdfBytes = await GenerateSertifikatPdf(sertifikat);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing certificate {CertificateId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat preview sertifikat.";
                return RedirectToAction(nameof(Sertifikat));
            }
        }

        private async Task<byte[]> GenerateSertifikatPdf(Sertifikat sertifikat)
        {
            using var memoryStream = new MemoryStream();
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4.Rotate());

            document.SetMargins(0, 0, 0, 0);

            var greenColor = new DeviceRgb(76, 175, 80);
            var blueColor = new DeviceRgb(13, 110, 253);
            var darkGrayColor = new DeviceRgb(102, 102, 102);
            var lightGrayBg = new DeviceRgb(245, 245, 245);

            var titleFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

            var page = pdf.AddNewPage();
            var pageSize = page.GetPageSize();
            var canvas = new PdfCanvas(page);

            canvas.SetFillColor(lightGrayBg)
                  .Rectangle(30, 100, pageSize.GetWidth() - 60, pageSize.GetHeight() - 200)
                  .Fill();

            var headerY = pageSize.GetHeight() - 100;
            var contentY = headerY - 80;

            document.ShowTextAligned(new Paragraph("SERTIFIKAT PELATIHAN")
                .SetFont(titleFont)
                .SetFontSize(32)
                .SetFontColor(blueColor),
                pageSize.GetWidth() / 2, contentY, TextAlignment.CENTER);

            contentY -= 40;
            document.ShowTextAligned(new Paragraph($"No. {sertifikat.NomorSertifikat}")
                .SetFont(bodyFont)
                .SetFontSize(14)
                .SetFontColor(darkGrayColor),
                pageSize.GetWidth() / 2, contentY, TextAlignment.CENTER);

            contentY -= 50;
            document.ShowTextAligned(new Paragraph("Diberikan kepada:")
                .SetFont(bodyFont)
                .SetFontSize(16)
                .SetFontColor(darkGrayColor),
                pageSize.GetWidth() / 2, contentY, TextAlignment.CENTER);

            contentY -= 40;
            document.ShowTextAligned(new Paragraph(sertifikat.User.Nama)
                .SetFont(titleFont)
                .SetFontSize(28)
                .SetFontColor(ColorConstants.BLACK),
                pageSize.GetWidth() / 2, contentY, TextAlignment.CENTER);

            contentY -= 10;
            var underlineLength = sertifikat.User.Nama.Length * 12;
            var startX = (pageSize.GetWidth() - underlineLength) / 2;
            canvas.SetStrokeColor(blueColor)
                  .SetLineWidth(2)
                  .MoveTo(startX, contentY)
                  .LineTo(startX + underlineLength, contentY)
                  .Stroke();

            contentY -= 50;
            document.ShowTextAligned(new Paragraph("Atas keberhasilannya menyelesaikan pelatihan")
                .SetFont(bodyFont)
                .SetFontSize(16)
                .SetFontColor(darkGrayColor),
                pageSize.GetWidth() / 2, contentY, TextAlignment.CENTER);

            contentY -= 40;
            document.ShowTextAligned(new Paragraph(sertifikat.Pelatihan.Judul)
                .SetFont(italicFont)
                .SetFontSize(22)
                .SetFontColor(blueColor),
                pageSize.GetWidth() / 2, contentY, TextAlignment.CENTER);

            var bottomY = 200;

            document.ShowTextAligned(new Paragraph("Diterbitkan pada")
                .SetFont(bodyFont)
                .SetFontSize(12)
                .SetFontColor(darkGrayColor),
                100, bottomY, TextAlignment.LEFT);

            document.ShowTextAligned(new Paragraph(sertifikat.TanggalTerbit.ToString("dd MMMM yyyy"))
                .SetFont(titleFont)
                .SetFontSize(14)
                .SetFontColor(ColorConstants.BLACK),
                100, bottomY - 20, TextAlignment.LEFT);

            document.ShowTextAligned(new Paragraph("Berlaku hingga")
                .SetFont(bodyFont)
                .SetFontSize(12)
                .SetFontColor(darkGrayColor),
                pageSize.GetWidth() - 100, bottomY, TextAlignment.RIGHT);

            var validityValue = sertifikat.TanggalKadaluarsa == DateTime.MaxValue ? "Selamanya" : sertifikat.TanggalKadaluarsa.ToString("dd MMMM yyyy");
            var validityColor = sertifikat.TanggalKadaluarsa == DateTime.MaxValue ? greenColor : ColorConstants.BLACK;

            document.ShowTextAligned(new Paragraph(validityValue)
                .SetFont(titleFont)
                .SetFontSize(14)
                .SetFontColor(validityColor),
                pageSize.GetWidth() - 100, bottomY - 20, TextAlignment.RIGHT);

            document.ShowTextAligned(new Paragraph("PT. Rama Express")
                .SetFont(bodyFont)
                .SetFontSize(12)
                .SetFontColor(darkGrayColor),
                pageSize.GetWidth() - 100, bottomY - 80, TextAlignment.RIGHT);

            document.Close();
            return memoryStream.ToArray();
        }
    }
}
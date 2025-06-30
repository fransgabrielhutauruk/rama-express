// Areas/Admin/Controllers/LaporanController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LaporanController : Controller
    {
        private readonly RamaExpressAppContext _context;

        public LaporanController(RamaExpressAppContext context)
        {
            _context = context;
        }

        [Route("Admin/Laporan")]
        public async Task<IActionResult> Index(
            string searchTerm = null,
            string pelatihanFilter = null,
            string statusFilter = null,
            string posisiFilter = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 10)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize));

            // Query base untuk mendapatkan data progress dan hasil pelatihan
            var query = from progress in _context.PelatihanProgress
                        join user in _context.User on progress.UserId equals user.Id
                        join pelatihan in _context.Pelatihan on progress.PelatihanId equals pelatihan.Id
                        where !user.IsDeleted && !pelatihan.IsDeleted
                        select new LaporanPelatihanViewModel
                        {
                            Id = progress.Id,
                            UserId = user.Id,
                            NamaKaryawan = user.Nama,
                            Email = user.Email,
                            Posisi = user.Posisi,
                            PelatihanId = pelatihan.Id,
                            KodePelatihan = pelatihan.Kode,
                            JudulPelatihan = pelatihan.Judul,
                            DurasiMenit = pelatihan.DurasiMenit,
                            SkorMinimal = pelatihan.SkorMinimal,
                            TanggalMulai = progress.StartedAt,
                            TanggalSelesai = progress.CompletedAt,
                            IsSelesai = progress.IsCompleted,
                            Skor = _context.PelatihanHasil
                                .Where(h => h.PelatihanId == progress.PelatihanId && h.UserId == progress.UserId)
                                .Select(h => (int?)h.Skor)
                                .FirstOrDefault(),
                            IsLulus = _context.PelatihanHasil
                                .Where(h => h.PelatihanId == progress.PelatihanId && h.UserId == progress.UserId)
                                .Select(h => (bool?)h.IsLulus)
                                .FirstOrDefault() ?? false
                        };

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(x => x.NamaKaryawan.Contains(searchTerm) ||
                                        x.Email.Contains(searchTerm) ||
                                        x.JudulPelatihan.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(pelatihanFilter))
            {
                if (int.TryParse(pelatihanFilter, out int pelatihanId))
                {
                    query = query.Where(x => x.PelatihanId == pelatihanId);
                }
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                switch (statusFilter.ToLower())
                {
                    case "selesai":
                        query = query.Where(x => x.IsSelesai);
                        break;
                    case "belum_selesai":
                        query = query.Where(x => !x.IsSelesai);
                        break;
                    case "lulus":
                        query = query.Where(x => x.IsLulus);
                        break;
                    case "tidak_lulus":
                        query = query.Where(x => x.IsSelesai && !x.IsLulus && x.Skor.HasValue);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(posisiFilter))
            {
                query = query.Where(x => x.Posisi == posisiFilter);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.TanggalMulai >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.TanggalMulai <= endDate.Value.AddDays(1));
            }

            var totalCount = await query.CountAsync();

            var results = await query
                .OrderByDescending(x => x.TanggalMulai)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get filter data
            var pelatihans = await _context.Pelatihan
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Judul)
                .ToListAsync();

            var posisis = await _context.User
                .Where(u => !u.IsDeleted && !string.IsNullOrEmpty(u.Posisi) && u.Role != "Admin")
                .Select(u => u.Posisi)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            // Calculate statistics
            var totalKaryawan = await _context.User
                .Where(u => !u.IsDeleted && u.Role.ToLower() == "karyawan")
                .CountAsync();

            var totalPelatihanAktif = await _context.Pelatihan
                .Where(p => !p.IsDeleted && p.IsActive)
                .CountAsync();

            var totalSertifikatTerbit = await _context.Sertifikat.CountAsync();

            var completionRate = totalCount > 0
                ? Math.Round((double)results.Count(r => r.IsSelesai) / totalCount * 100, 1)
                : 0;

            var viewModel = new LaporanPelatihanListViewModel
            {
                Results = results,
                CurrentPage = page,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                PageSize = pageSize,
                SearchTerm = searchTerm,
                PelatihanFilter = pelatihanFilter,
                StatusFilter = statusFilter,
                PosisiFilter = posisiFilter,
                StartDate = startDate,
                EndDate = endDate,

                // Filter data
                AvailablePelatihans = pelatihans,
                AvailablePosisis = posisis,

                // Statistics
                TotalKaryawan = totalKaryawan,
                TotalPelatihanAktif = totalPelatihanAktif,
                TotalSertifikatTerbit = totalSertifikatTerbit,
                CompletionRate = completionRate
            };

            return View(viewModel);
        }

        [Route("Admin/Laporan/Detail/{userId}/{pelatihanId}")]
        public async Task<IActionResult> Detail(int userId, int pelatihanId)
        {
            var progress = await _context.PelatihanProgress
                .Include(p => p.User)
                .Include(p => p.Pelatihan)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PelatihanId == pelatihanId);

            if (progress == null)
            {
                TempData["ErrorMessage"] = "Data progress tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var hasil = await _context.PelatihanHasil
                .FirstOrDefaultAsync(h => h.UserId == userId && h.PelatihanId == pelatihanId);

            var sertifikat = await _context.Sertifikat
                .FirstOrDefaultAsync(s => s.UserId == userId && s.PelatihanId == pelatihanId);

            var materials = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == pelatihanId)
                .OrderBy(m => m.Urutan)
                .ToListAsync();

            var questions = await _context.PelatihanSoal
                .Where(q => q.PelatihanId == pelatihanId)
                .OrderBy(q => q.Urutan)
                .ToListAsync();

            var viewModel = new LaporanDetailViewModel
            {
                Progress = progress,
                Hasil = hasil,
                Sertifikat = sertifikat,
                Materials = materials,
                Questions = questions
            };

            return View(viewModel);
        }

        [Route("Admin/Laporan/Export")]
        public async Task<IActionResult> Export(
            string searchTerm = null,
            string pelatihanFilter = null,
            string statusFilter = null,
            string posisiFilter = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string format = "csv")
        {
            // Get filtered data (same query as Index but without pagination)
            var query = from progress in _context.PelatihanProgress
                        join user in _context.User on progress.UserId equals user.Id
                        join pelatihan in _context.Pelatihan on progress.PelatihanId equals pelatihan.Id
                        where !user.IsDeleted && !pelatihan.IsDeleted
                        select new LaporanPelatihanViewModel
                        {
                            NamaKaryawan = user.Nama,
                            Email = user.Email,
                            Posisi = user.Posisi,
                            KodePelatihan = pelatihan.Kode,
                            JudulPelatihan = pelatihan.Judul,
                            DurasiMenit = pelatihan.DurasiMenit,
                            SkorMinimal = pelatihan.SkorMinimal,
                            TanggalMulai = progress.StartedAt,
                            TanggalSelesai = progress.CompletedAt,
                            IsSelesai = progress.IsCompleted,
                            Skor = _context.PelatihanHasil
                                .Where(h => h.PelatihanId == progress.PelatihanId && h.UserId == progress.UserId)
                                .Select(h => (int?)h.Skor)
                                .FirstOrDefault(),
                            IsLulus = _context.PelatihanHasil
                                .Where(h => h.PelatihanId == progress.PelatihanId && h.UserId == progress.UserId)
                                .Select(h => (bool?)h.IsLulus)
                                .FirstOrDefault() ?? false
                        };

            // Apply same filters as Index
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(x => x.NamaKaryawan.Contains(searchTerm) ||
                                        x.Email.Contains(searchTerm) ||
                                        x.JudulPelatihan.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(pelatihanFilter))
            {
                if (int.TryParse(pelatihanFilter, out int pelatihanId))
                {
                    query = query.Where(x => x.PelatihanId == pelatihanId);
                }
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                switch (statusFilter.ToLower())
                {
                    case "selesai":
                        query = query.Where(x => x.IsSelesai);
                        break;
                    case "belum_selesai":
                        query = query.Where(x => !x.IsSelesai);
                        break;
                    case "lulus":
                        query = query.Where(x => x.IsLulus);
                        break;
                    case "tidak_lulus":
                        query = query.Where(x => x.IsSelesai && !x.IsLulus);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(posisiFilter))
            {
                query = query.Where(x => x.Posisi == posisiFilter);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.TanggalMulai >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.TanggalMulai <= endDate.Value.AddDays(1));
            }

            var results = await query
                .OrderByDescending(x => x.TanggalMulai)
                .ToListAsync();

            if (format.ToLower() == "csv")
            {
                return ExportToCsv(results);
            }
            else
            {
                // Default to CSV if format not recognized
                return ExportToCsv(results);
            }
        }

        private IActionResult ExportToCsv(List<LaporanPelatihanViewModel> data)
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine("Nama Karyawan,Email,Posisi,Kode Pelatihan,Judul Pelatihan,Durasi (Menit),Skor Minimal,Tanggal Mulai,Tanggal Selesai,Status,Skor,Hasil");

            // Data rows
            foreach (var item in data)
            {
                csv.AppendLine($"\"{item.NamaKaryawan}\",\"{item.Email}\",\"{item.Posisi}\",\"{item.KodePelatihan}\",\"{item.JudulPelatihan}\",{item.DurasiMenit},{item.SkorMinimal},\"{item.TanggalMulai:yyyy-MM-dd HH:mm}\",\"{(item.TanggalSelesai?.ToString("yyyy-MM-dd HH:mm") ?? "Belum Selesai")}\",\"{(item.IsSelesai ? "Selesai" : "Belum Selesai")}\",{item.Skor?.ToString() ?? "0"},\"{(item.IsLulus ? "Lulus" : "Tidak Lulus")}\"");
            }

            var fileName = $"laporan_pelatihan_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

            return File(bytes, "text/csv", fileName);
        }
    }
}
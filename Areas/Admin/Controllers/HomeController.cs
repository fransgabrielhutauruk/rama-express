using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using System.Globalization;

namespace RamaExpress.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly RamaExpressAppContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(RamaExpressAppContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Route("Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get dashboard statistics
                var dashboardData = await GetDashboardStatistics();
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                // Log the error properly
                _logger.LogError(ex, "Error loading dashboard data");

                // Return view with empty data
                var emptyData = new DashboardViewModel();
                return View(emptyData);
            }
        }

        [Route("Admin/Setting")]
        public IActionResult Setting()
        {
            return View();
        }

        [HttpGet]
        [Route("Admin/Api/Dashboard/Stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await GetDashboardStatistics();
                return Json(new
                {
                    success = true,
                    totalEmployees = stats.TotalEmployees,
                    activeEmployees = stats.ActiveEmployees,
                    inactiveEmployees = stats.InactiveEmployees,
                    activeTrainings = stats.ActiveTrainings,
                    totalTrainings = stats.TotalTrainings,
                    totalCertificates = stats.TotalCertificates,
                    monthlyIssuedCertificates = stats.MonthlyIssuedCertificates,
                    completionRate = stats.CompletionRate,
                    completedTrainings = stats.CompletedTrainings,
                    totalProgress = stats.TotalProgress,
                    activeEmployeePercentage = stats.ActiveEmployeePercentage,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new
                {
                    success = false,
                    error = "Gagal memuat statistik dashboard",
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpGet]
        [Route("Admin/Api/Dashboard/Charts")]
        public async Task<IActionResult> GetChartData()
        {
            try
            {
                var chartData = await GetChartsData();

                // Calculate percentages for position distribution
                var totalPositionCount = chartData.PositionDistribution.Sum(x => x.Count);
                foreach (var position in chartData.PositionDistribution)
                {
                    position.Percentage = totalPositionCount > 0 ?
                        Math.Round((double)position.Count / totalPositionCount * 100, 1) : 0;
                }

                return Json(new
                {
                    success = true,
                    monthlyProgress = chartData.MonthlyProgress.Select(m => new
                    {
                        month = m.Month,
                        completedTrainings = m.CompletedTrainings,
                        issuedCertificates = m.IssuedCertificates,
                        certificateRatio = m.CertificateToCompletionRatio
                    }),
                    positionDistribution = chartData.PositionDistribution.Select(p => new
                    {
                        position = p.Position,
                        count = p.Count,
                        percentage = p.Percentage
                    }),
                    summary = new
                    {
                        totalCompletions = chartData.TotalMonthlyCompletions,
                        totalCertificates = chartData.TotalMonthlyCertificates,
                        hasData = chartData.HasProgressData && chartData.HasPositionData
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data");
                return Json(new
                {
                    success = false,
                    error = "Gagal memuat data chart",
                    timestamp = DateTime.Now
                });
            }
        }

        private async Task<DashboardViewModel> GetDashboardStatistics()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            try
            {
                // Employee statistics
                var totalEmployees = await _context.User
                    .Where(u => u.Role.ToLower() == "karyawan" && !u.IsDeleted)
                    .CountAsync();

                var activeEmployees = await _context.User
                    .Where(u => u.Role.ToLower() == "karyawan" && !u.IsDeleted && u.IsActive)
                    .CountAsync();

                var inactiveEmployees = totalEmployees - activeEmployees;

                // Training statistics
                var totalTrainings = await _context.Pelatihan
                    .Where(p => !p.IsDeleted)
                    .CountAsync();

                var activeTrainings = await _context.Pelatihan
                    .Where(p => !p.IsDeleted && p.IsActive)
                    .CountAsync();

                // Certificate statistics
                var totalCertificates = await _context.Sertifikat.CountAsync();

                var monthlyIssuedCertificates = await _context.Sertifikat
                    .Where(s => s.TanggalTerbit.Month == currentMonth && s.TanggalTerbit.Year == currentYear)
                    .CountAsync();

                // Training completion statistics
                var totalProgress = await _context.PelatihanProgress.CountAsync();
                var completedTrainings = await _context.PelatihanProgress
                    .Where(p => p.IsCompleted)
                    .CountAsync();

                var completionRate = totalProgress > 0 ? Math.Round((double)completedTrainings / totalProgress * 100, 1) : 0;

                return new DashboardViewModel
                {
                    TotalEmployees = totalEmployees,
                    ActiveEmployees = activeEmployees,
                    InactiveEmployees = inactiveEmployees,
                    ActiveTrainings = activeTrainings,
                    TotalTrainings = totalTrainings,
                    TotalCertificates = totalCertificates,
                    MonthlyIssuedCertificates = monthlyIssuedCertificates,
                    CompletionRate = completionRate,
                    CompletedTrainings = completedTrainings,
                    TotalProgress = totalProgress
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardStatistics");
                throw;
            }
        }

        private async Task<ChartDataViewModel> GetChartsData()
        {
            try
            {
                // Training progress data for the last 6 months
                var monthlyData = new List<MonthlyProgressData>();

                // Set culture to Indonesian for month names
                var culture = new CultureInfo("id-ID");

                for (int i = 5; i >= 0; i--)
                {
                    var month = DateTime.Now.AddMonths(-i);
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var completedTrainings = await _context.PelatihanProgress
                        .Where(p => p.IsCompleted && p.CompletedAt.HasValue &&
                                   p.CompletedAt.Value >= monthStart && p.CompletedAt.Value <= monthEnd)
                        .CountAsync();

                    var issuedCertificates = await _context.Sertifikat
                        .Where(s => s.TanggalTerbit >= monthStart && s.TanggalTerbit <= monthEnd)
                        .CountAsync();

                    monthlyData.Add(new MonthlyProgressData
                    {
                        Month = month.ToString("MMMM", culture),
                        CompletedTrainings = completedTrainings,
                        IssuedCertificates = issuedCertificates
                    });
                }

                // Position distribution data
                var positionData = await _context.User
                    .Where(u => u.Role.ToLower() == "karyawan" && !u.IsDeleted && !string.IsNullOrEmpty(u.Posisi))
                    .GroupBy(u => u.Posisi)
                    .Select(g => new PositionDistributionData
                    {
                        Position = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                return new ChartDataViewModel
                {
                    MonthlyProgress = monthlyData,
                    PositionDistribution = positionData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetChartsData");
                throw;
            }
        }
    }
}
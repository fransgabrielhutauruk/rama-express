// RamaExpress.Tests/Controllers/Admin/HomeControllerTests.cs - CORRECTED VERSION
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using RamaExpress.Areas.Admin.Controllers;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using System.Globalization;

namespace RamaExpress.Tests.Controllers.Admin
{
    public class HomeControllerTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);

            // Setup mocks
            _mockLogger = new Mock<ILogger<HomeController>>();

            // Create controller
            _controller = new HomeController(_context, _mockLogger.Object);

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        public void Dispose()
        {
            _context.Dispose();
            _controller?.Dispose();
        }

        #region Index Tests

        [Fact]
        public async Task Index_WithValidData_ReturnsViewWithDashboardViewModel()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<DashboardViewModel>();

            var model = viewResult.Model as DashboardViewModel;
            model!.TotalEmployees.Should().Be(3);
            model.ActiveEmployees.Should().Be(2);
            model.InactiveEmployees.Should().Be(1);
            model.TotalTrainings.Should().Be(2);
            model.ActiveTrainings.Should().Be(1);
        }

        [Fact]
        public async Task Index_WithEmptyDatabase_ReturnsViewWithZeroStatistics()
        {
            // Arrange
            // No test data seeded

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<DashboardViewModel>();

            var model = viewResult.Model as DashboardViewModel;
            model!.TotalEmployees.Should().Be(0);
            model.ActiveEmployees.Should().Be(0);
            model.InactiveEmployees.Should().Be(0);
            model.TotalTrainings.Should().Be(0);
            model.ActiveTrainings.Should().Be(0);
            model.TotalCertificates.Should().Be(0);
        }

        [Fact]
        public async Task Index_WithDatabaseError_ReturnsViewWithEmptyData()
        {
            // Arrange
            _context.Dispose(); // Force database error

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<DashboardViewModel>();

            var model = viewResult.Model as DashboardViewModel;
            model!.TotalEmployees.Should().Be(0);
            model.ActiveEmployees.Should().Be(0);

            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error loading dashboard data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Setting Tests

        [Fact]
        public void Setting_ReturnsView()
        {
            // Act
            var result = _controller.Setting();

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        #endregion

        #region GetDashboardStats API Tests

        [Fact]
        public async Task GetDashboardStats_WithValidData_ReturnsJsonWithCorrectStatistics()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            jsonResult!.Value.Should().NotBeNull();

            // Convert to dynamic to check properties
            var data = jsonResult.Value as dynamic;
            var dict = jsonResult.Value.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            dict["success"].Should().Be(true);
            dict["totalEmployees"].Should().Be(3);
            dict["activeEmployees"].Should().Be(2);
            dict["inactiveEmployees"].Should().Be(1);
            dict["totalTrainings"].Should().Be(2);
            dict["activeTrainings"].Should().Be(1);
            dict["timestamp"].Should().NotBeNull();
        }

        [Fact]
        public async Task GetDashboardStats_WithDatabaseError_ReturnsErrorJson()
        {
            // Arrange
            _context.Dispose(); // Force database error

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var dict = jsonResult!.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            dict["success"].Should().Be(false);
            dict["error"].Should().Be("Gagal memuat statistik dashboard");
            dict["timestamp"].Should().NotBeNull();

            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting dashboard stats")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetDashboardStats_CalculatesCompletionRateCorrectly()
        {
            // Arrange
            await SeedTestDataWithProgress();

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var dict = jsonResult!.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            // 2 completed out of 3 total progress = 66.7%
            dict["completionRate"].Should().Be(66.7);
            dict["completedTrainings"].Should().Be(2);
            dict["totalProgress"].Should().Be(3);
        }

        #endregion

        #region GetChartData API Tests

        [Fact]
        public async Task GetChartData_WithValidData_ReturnsJsonWithChartData()
        {
            // Arrange
            await SeedTestDataWithChartData();

            // Act
            var result = await _controller.GetChartData();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            jsonResult!.Value.Should().NotBeNull();

            var dict = jsonResult.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            dict["success"].Should().Be(true);
            dict["monthlyProgress"].Should().NotBeNull();
            dict["positionDistribution"].Should().NotBeNull();
            dict["summary"].Should().NotBeNull();
            dict["timestamp"].Should().NotBeNull();
        }

        [Fact]
        public async Task GetChartData_WithDatabaseError_ReturnsErrorJson()
        {
            // Arrange
            _context.Dispose(); // Force database error

            // Act
            var result = await _controller.GetChartData();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var dict = jsonResult!.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            dict["success"].Should().Be(false);
            dict["error"].Should().Be("Gagal memuat data chart");

            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting chart data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetChartData_CalculatesPositionPercentagesCorrectly()
        {
            // Arrange
            await SeedTestDataWithMultiplePositions();

            // Act
            var result = await _controller.GetChartData();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var dict = jsonResult!.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            dict["success"].Should().Be(true);

            var positionDistribution = dict["positionDistribution"] as IEnumerable<object>;
            positionDistribution.Should().NotBeNull();
            positionDistribution!.Should().HaveCount(3); // Developer, Tester, Manager
        }

        [Fact]
        public async Task GetChartData_GeneratesLast6MonthsData()
        {
            // Arrange
            await SeedTestDataWithMonthlyProgress();

            // Act
            var result = await _controller.GetChartData();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var dict = jsonResult!.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            var monthlyProgress = dict["monthlyProgress"] as IEnumerable<object>;
            monthlyProgress.Should().NotBeNull();
            monthlyProgress!.Should().HaveCount(6); // Last 6 months
        }

        #endregion

        #region Monthly Statistics Tests

        [Fact]
        public async Task GetDashboardStats_CalculatesMonthlyStatisticsCorrectly()
        {
            // Arrange
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            await SeedTestDataWithCurrentMonthData(currentMonth, currentYear);

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var dict = jsonResult!.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            dict["monthlyIssuedCertificates"].Should().Be(2); // Current month certificates
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task GetDashboardStats_WithZeroProgress_ReturnsZeroCompletionRate()
        {
            // Arrange
            await SeedTestDataWithoutProgress();

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var dict = jsonResult!.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            dict["completionRate"].Should().Be(0);
            dict["totalProgress"].Should().Be(0);
            dict["completedTrainings"].Should().Be(0);
        }

        [Fact]
        public async Task GetChartData_WithNoEmployees_ReturnsEmptyPositionDistribution()
        {
            // Arrange
            // No employees seeded

            // Act
            var result = await _controller.GetChartData();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var dict = jsonResult!.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(jsonResult.Value));

            var positionDistribution = dict["positionDistribution"] as IEnumerable<object>;
            positionDistribution.Should().NotBeNull();
            positionDistribution!.Should().BeEmpty();
        }

        [Fact]
        public async Task Index_WithOnlyAdminUsers_FiltersCorrectly()
        {
            // Arrange
            await SeedTestDataWithAdminUsers();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as DashboardViewModel;

            model!.TotalEmployees.Should().Be(0); // Should only count karyawan role
            model.ActiveEmployees.Should().Be(0);
        }

        #endregion

        #region Helper Methods

        private async Task SeedTestData()
        {
            // Users with karyawan role
            var users = new List<User>
            {
                new() { Id = 1, Nama = "John Doe", Email = "john@test.com", Role = "karyawan", Posisi = "Developer", IsActive = true, IsDeleted = false },
                new() { Id = 2, Nama = "Jane Smith", Email = "jane@test.com", Role = "karyawan", Posisi = "Tester", IsActive = true, IsDeleted = false },
                new() { Id = 3, Nama = "Bob Wilson", Email = "bob@test.com", Role = "karyawan", Posisi = "Developer", IsActive = false, IsDeleted = false }
            };

            // Trainings
            var trainings = new List<Pelatihan>
            {
                new() { Id = 1, Kode = "TR001", Judul = "Training 1", IsActive = true, IsDeleted = false, CreatedAt = DateTime.Now },
                new() { Id = 2, Kode = "TR002", Judul = "Training 2", IsActive = false, IsDeleted = false, CreatedAt = DateTime.Now }
            };

            // Certificates
            var certificates = new List<Sertifikat>
            {
                new() { Id = 1, UserId = 1, PelatihanId = 1, NomorSertifikat = "CERT001", TanggalTerbit = DateTime.Now, TanggalKadaluarsa = DateTime.MaxValue },
                new() { Id = 2, UserId = 2, PelatihanId = 1, NomorSertifikat = "CERT002", TanggalTerbit = DateTime.Now, TanggalKadaluarsa = DateTime.MaxValue }
            };

            _context.User.AddRange(users);
            _context.Pelatihan.AddRange(trainings);
            _context.Sertifikat.AddRange(certificates);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithProgress()
        {
            await SeedTestData();

            var progresses = new List<PelatihanProgress>
            {
                new() { Id = 1, UserId = 1, PelatihanId = 1, IsCompleted = true, StartedAt = DateTime.Now, CompletedAt = DateTime.Now },
                new() { Id = 2, UserId = 2, PelatihanId = 1, IsCompleted = true, StartedAt = DateTime.Now, CompletedAt = DateTime.Now },
                new() { Id = 3, UserId = 3, PelatihanId = 2, IsCompleted = false, StartedAt = DateTime.Now }
            };

            _context.PelatihanProgress.AddRange(progresses);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithChartData()
        {
            await SeedTestData();

            // Add some progress and certificates for chart data
            var progresses = new List<PelatihanProgress>
            {
                new() { Id = 1, UserId = 1, PelatihanId = 1, IsCompleted = true, StartedAt = DateTime.Now.AddMonths(-1), CompletedAt = DateTime.Now.AddMonths(-1) }
            };

            var certificates = new List<Sertifikat>
            {
                new() { Id = 3, UserId = 1, PelatihanId = 1, NomorSertifikat = "CERT003", TanggalTerbit = DateTime.Now.AddMonths(-1), TanggalKadaluarsa = DateTime.MaxValue }
            };

            _context.PelatihanProgress.AddRange(progresses);
            _context.Sertifikat.AddRange(certificates);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithMultiplePositions()
        {
            var users = new List<User>
            {
                new() { Id = 1, Nama = "Dev1", Email = "dev1@test.com", Role = "karyawan", Posisi = "Developer", IsActive = true, IsDeleted = false },
                new() { Id = 2, Nama = "Dev2", Email = "dev2@test.com", Role = "karyawan", Posisi = "Developer", IsActive = true, IsDeleted = false },
                new() { Id = 3, Nama = "Test1", Email = "test1@test.com", Role = "karyawan", Posisi = "Tester", IsActive = true, IsDeleted = false },
                new() { Id = 4, Nama = "Mgr1", Email = "mgr1@test.com", Role = "karyawan", Posisi = "Manager", IsActive = true, IsDeleted = false }
            };

            _context.User.AddRange(users);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithMonthlyProgress()
        {
            await SeedTestData();

            var progresses = new List<PelatihanProgress>();
            var certificates = new List<Sertifikat>();

            for (int i = 1; i <= 6; i++)
            {
                var date = DateTime.Now.AddMonths(-i);
                progresses.Add(new PelatihanProgress
                {
                    Id = i,
                    UserId = 1,
                    PelatihanId = 1,
                    IsCompleted = true,
                    StartedAt = date,
                    CompletedAt = date
                });

                certificates.Add(new Sertifikat
                {
                    Id = i + 10,
                    UserId = 1,
                    PelatihanId = 1,
                    NomorSertifikat = $"CERT{i:000}",
                    TanggalTerbit = date,
                    TanggalKadaluarsa = DateTime.MaxValue
                });
            }

            _context.PelatihanProgress.AddRange(progresses);
            _context.Sertifikat.AddRange(certificates);
            await _context.SaveChangesAsync();
        }

        // ✅ FIXED: This method now creates its own data without calling SeedTestData()
        private async Task SeedTestDataWithCurrentMonthData(int month, int year)
        {
            // Create basic data without calling SeedTestData() to avoid duplicate certificates
            var users = new List<User>
            {
                new() { Id = 1, Nama = "John Doe", Email = "john@test.com", Role = "karyawan", Posisi = "Developer", IsActive = true, IsDeleted = false },
                new() { Id = 2, Nama = "Jane Smith", Email = "jane@test.com", Role = "karyawan", Posisi = "Tester", IsActive = true, IsDeleted = false }
            };

            var trainings = new List<Pelatihan>
            {
                new() { Id = 1, Kode = "TR001", Judul = "Training 1", IsActive = true, IsDeleted = false, CreatedAt = DateTime.Now }
            };

            var currentMonthDate = new DateTime(year, month, 15);

            // Only add certificates for the current month (not calling SeedTestData())
            var certificates = new List<Sertifikat>
            {
                new() { Id = 1, UserId = 1, PelatihanId = 1, NomorSertifikat = "CERT001", TanggalTerbit = currentMonthDate, TanggalKadaluarsa = DateTime.MaxValue },
                new() { Id = 2, UserId = 2, PelatihanId = 1, NomorSertifikat = "CERT002", TanggalTerbit = currentMonthDate, TanggalKadaluarsa = DateTime.MaxValue }
            };

            _context.User.AddRange(users);
            _context.Pelatihan.AddRange(trainings);
            _context.Sertifikat.AddRange(certificates);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithoutProgress()
        {
            // Only seed basic data without any progress records
            var users = new List<User>
            {
                new() { Id = 1, Nama = "John Doe", Email = "john@test.com", Role = "karyawan", Posisi = "Developer", IsActive = true, IsDeleted = false }
            };

            var trainings = new List<Pelatihan>
            {
                new() { Id = 1, Kode = "TR001", Judul = "Training 1", IsActive = true, IsDeleted = false, CreatedAt = DateTime.Now }
            };

            _context.User.AddRange(users);
            _context.Pelatihan.AddRange(trainings);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithAdminUsers()
        {
            var users = new List<User>
            {
                new() { Id = 1, Nama = "Admin 1", Email = "admin1@test.com", Role = "admin", Posisi = "Administrator", IsActive = true, IsDeleted = false },
                new() { Id = 2, Nama = "Admin 2", Email = "admin2@test.com", Role = "admin", Posisi = "Super Admin", IsActive = true, IsDeleted = false }
            };

            _context.User.AddRange(users);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}

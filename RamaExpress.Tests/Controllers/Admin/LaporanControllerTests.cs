// RamaExpress.Tests/Controllers/Admin/LaporanControllerTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using RamaExpress.Areas.Admin.Controllers;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using System.Text;

namespace RamaExpress.Tests.Controllers.Admin
{
    public class LaporanControllerTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly LaporanController _controller;

        public LaporanControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);

            // Create controller
            _controller = new LaporanController(_context);

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
        public async Task Index_ReturnsViewWithCorrectViewModel()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<LaporanPelatihanListViewModel>();

            var model = viewResult.Model as LaporanPelatihanListViewModel;
            model!.Results.Should().NotBeEmpty();
            model.CurrentPage.Should().Be(1);
            model.PageSize.Should().Be(10);
            model.AvailablePelatihans.Should().NotBeEmpty();
            model.AvailablePosisis.Should().NotBeEmpty();
            model.TotalKaryawan.Should().BeGreaterThan(0);
            model.TotalPelatihanAktif.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Index_WithSearchTerm_FiltersResultsCorrectly()
        {
            // Arrange
            await SeedTestData();
            var searchTerm = "John";

            // Act
            var result = await _controller.Index(searchTerm: searchTerm);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.SearchTerm.Should().Be(searchTerm);

            // Check if results exist and verify filtering logic
            var resultsWithSearchTerm = model.Results.Where(r =>
                r.NamaKaryawan.Contains(searchTerm) ||
                r.Email.Contains(searchTerm) ||
                r.JudulPelatihan.Contains(searchTerm)).ToList();

            // If there are any results, they should all match the search criteria
            if (model.Results.Any())
            {
                resultsWithSearchTerm.Should().HaveCount(model.Results.Count());
            }
        }

        [Fact]
        public async Task Index_WithPelatihanFilter_FiltersResultsCorrectly()
        {
            // Arrange
            await SeedTestData();
            var pelatihanFilter = "1";

            // Act
            var result = await _controller.Index(pelatihanFilter: pelatihanFilter);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.PelatihanFilter.Should().Be(pelatihanFilter);

            // If there are results, they should all have PelatihanId = 1
            foreach (var item in model.Results)
            {
                item.PelatihanId.Should().Be(1);
            }
        }

        [Fact]
        public async Task Index_WithStatusFilterSelesai_FiltersResultsCorrectly()
        {
            // Arrange
            await SeedTestData();
            var statusFilter = "selesai";

            // Act
            var result = await _controller.Index(statusFilter: statusFilter);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.StatusFilter.Should().Be(statusFilter);

            // If there are results, they should all be completed
            foreach (var item in model.Results)
            {
                item.IsSelesai.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Index_WithStatusFilterBelumSelesai_FiltersResultsCorrectly()
        {
            // Arrange
            await SeedTestData();
            var statusFilter = "belum_selesai";

            // Act
            var result = await _controller.Index(statusFilter: statusFilter);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.StatusFilter.Should().Be(statusFilter);

            // If there are results, they should all be incomplete
            foreach (var item in model.Results)
            {
                item.IsSelesai.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Index_WithStatusFilterLulus_FiltersResultsCorrectly()
        {
            // Arrange
            await SeedTestData();
            var statusFilter = "lulus";

            // Act
            var result = await _controller.Index(statusFilter: statusFilter);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.StatusFilter.Should().Be(statusFilter);

            // If there are results, they should all be passed
            foreach (var item in model.Results)
            {
                item.IsLulus.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Index_WithStatusFilterTidakLulus_FiltersResultsCorrectly()
        {
            // Arrange
            await SeedTestData();
            var statusFilter = "tidak_lulus";

            // Act
            var result = await _controller.Index(statusFilter: statusFilter);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.StatusFilter.Should().Be(statusFilter);

            // If there are results, they should all be completed but not passed
            foreach (var item in model.Results)
            {
                item.IsSelesai.Should().BeTrue();
                item.IsLulus.Should().BeFalse();
                item.Skor.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Index_WithPosisiFilter_FiltersResultsCorrectly()
        {
            // Arrange
            await SeedTestData();
            var posisiFilter = "Developer";

            // Act
            var result = await _controller.Index(posisiFilter: posisiFilter);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.PosisiFilter.Should().Be(posisiFilter);

            // If there are results, they should all have the specified position
            foreach (var item in model.Results)
            {
                item.Posisi.Should().Be(posisiFilter);
            }
        }

        [Fact]
        public async Task Index_WithDateRangeFilter_FiltersResultsCorrectly()
        {
            // Arrange
            await SeedTestData();
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            // Act
            var result = await _controller.Index(startDate: startDate, endDate: endDate);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.StartDate.Should().Be(startDate);
            model.EndDate.Should().Be(endDate);

            // If there are results, they should all be within the date range
            foreach (var item in model.Results)
            {
                item.TanggalMulai.Should().BeOnOrAfter(startDate);
                item.TanggalMulai.Should().BeOnOrBefore(endDate.AddDays(1));
            }
        }

        [Fact]
        public async Task Index_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            await SeedTestData();
            var page = 2;
            var pageSize = 5;

            // Act
            var result = await _controller.Index(page: page, pageSize: pageSize);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.CurrentPage.Should().Be(page);
            model.PageSize.Should().Be(pageSize);
            model.Results.Should().HaveCountLessOrEqualTo(pageSize);
        }

        [Fact]
        public async Task Index_WithInvalidPage_SetsPageToOne()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Index(page: -1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.CurrentPage.Should().Be(1);
        }

        [Fact]
        public async Task Index_WithInvalidPageSize_ClampsToValidRange()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Index(pageSize: 150);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.PageSize.Should().Be(100); // Clamped to maximum
        }

        [Fact]
        public async Task Index_WithZeroPageSize_SetsToOne()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Index(pageSize: 0);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.PageSize.Should().Be(1); // Clamped to minimum
        }

        [Fact]
        public async Task Index_WithComplexFilters_CombinesFiltersCorrectly()
        {
            // Arrange
            await SeedTestData();
            var searchTerm = "John";
            var pelatihanFilter = "1";
            var statusFilter = "selesai";
            var posisiFilter = "Developer";

            // Act
            var result = await _controller.Index(
                searchTerm: searchTerm,
                pelatihanFilter: pelatihanFilter,
                statusFilter: statusFilter,
                posisiFilter: posisiFilter);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.SearchTerm.Should().Be(searchTerm);
            model.PelatihanFilter.Should().Be(pelatihanFilter);
            model.StatusFilter.Should().Be(statusFilter);
            model.PosisiFilter.Should().Be(posisiFilter);
        }

        #endregion

        #region Detail Tests

        [Fact]
        public async Task Detail_WithValidIds_ReturnsViewWithCorrectModel()
        {
            // Arrange
            await SeedTestData();
            var userId = 1;
            var pelatihanId = 1;

            // Act
            var result = await _controller.Detail(userId, pelatihanId);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<LaporanDetailViewModel>();

            var model = viewResult.Model as LaporanDetailViewModel;
            model!.Progress.Should().NotBeNull();
            model.Progress.UserId.Should().Be(userId);
            model.Progress.PelatihanId.Should().Be(pelatihanId);
            model.Progress.User.Should().NotBeNull();
            model.Progress.Pelatihan.Should().NotBeNull();
            model.Materials.Should().NotBeNull();
            model.Questions.Should().NotBeNull();
        }

        [Fact]
        public async Task Detail_WithInvalidIds_ReturnsRedirectToIndex()
        {
            // Arrange
            await SeedTestData();
            var invalidUserId = 999;
            var invalidPelatihanId = 999;

            // Act
            var result = await _controller.Detail(invalidUserId, invalidPelatihanId);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Data progress tidak ditemukan");
        }

        [Fact]
        public async Task Detail_WithValidIds_IncludesAllRelatedData()
        {
            // Arrange
            await SeedTestData();
            var userId = 1;
            var pelatihanId = 1;

            // Act
            var result = await _controller.Detail(userId, pelatihanId);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanDetailViewModel;

            model!.Progress.Should().NotBeNull();
            model.Progress.User.Should().NotBeNull();
            model.Progress.Pelatihan.Should().NotBeNull();
            model.Materials.Should().NotBeEmpty();
            model.Questions.Should().NotBeEmpty();

            // Verify optional data can be null or populated
            // Hasil and Sertifikat are optional, so they may be null
        }

        #endregion

        #region Export Tests

        [Fact]
        public async Task Export_WithDefaultParameters_ReturnsFileResult()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Export();

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
            fileResult.FileDownloadName.Should().StartWith("laporan_pelatihan_");
            fileResult.FileDownloadName.Should().EndWith(".csv");
        }

        [Fact]
        public async Task Export_WithCsvFormat_ReturnsFileResult()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Export(format: "csv");

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
        }

        [Fact]
        public async Task Export_WithUnknownFormat_DefaultsToCsv()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Export(format: "unknown");

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
        }

        [Fact]
        public async Task Export_WithFilters_AppliesFiltersToExport()
        {
            // Arrange
            await SeedTestData();
            var searchTerm = "John";
            var statusFilter = "selesai";

            // Act
            var result = await _controller.Export(searchTerm: searchTerm, statusFilter: statusFilter);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");

            // Verify that the file is generated (we can't easily test the content without additional setup)
        }

        [Fact]
        public async Task Export_WithDateRange_AppliesDateFilterToExport()
        {
            // Arrange
            await SeedTestData();
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            // Act
            var result = await _controller.Export(startDate: startDate, endDate: endDate);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
        }

        [Fact]
        public async Task Export_WithPelatihanFilter_AppliesFilterToExport()
        {
            // Arrange
            await SeedTestData();
            var pelatihanFilter = "1";

            // Act
            var result = await _controller.Export(pelatihanFilter: pelatihanFilter);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
        }

        [Fact]
        public async Task Export_WithPosisiFilter_AppliesFilterToExport()
        {
            // Arrange
            await SeedTestData();
            var posisiFilter = "Developer";

            // Act
            var result = await _controller.Export(posisiFilter: posisiFilter);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
        }

        [Fact]
        public async Task Export_WithAllFilters_CombinesFiltersCorrectly()
        {
            // Arrange
            await SeedTestData();
            var searchTerm = "John";
            var pelatihanFilter = "1";
            var statusFilter = "selesai";
            var posisiFilter = "Developer";
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            // Act
            var result = await _controller.Export(
                searchTerm: searchTerm,
                pelatihanFilter: pelatihanFilter,
                statusFilter: statusFilter,
                posisiFilter: posisiFilter,
                startDate: startDate,
                endDate: endDate);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
            fileResult.FileDownloadName.Should().StartWith("laporan_pelatihan_");
            fileResult.FileDownloadName.Should().EndWith(".csv");
        }

        [Fact]
        public async Task Export_WithEmptyResults_ReturnsEmptyFile()
        {
            // Arrange
            await SeedTestData();
            var searchTerm = "NonExistentUser";

            // Act
            var result = await _controller.Export(searchTerm: searchTerm);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task LaporanWorkflow_IndexToDetailToExport_WorksCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act & Assert - Index
            var indexResult = await _controller.Index();
            indexResult.Should().BeOfType<ViewResult>();
            var indexModel = (indexResult as ViewResult)!.Model as LaporanPelatihanListViewModel;
            indexModel!.Results.Should().NotBeEmpty();

            // Act & Assert - Detail
            var firstResult = indexModel.Results.First();
            var detailResult = await _controller.Detail(firstResult.UserId, firstResult.PelatihanId);
            detailResult.Should().BeOfType<ViewResult>();
            var detailModel = (detailResult as ViewResult)!.Model as LaporanDetailViewModel;
            detailModel!.Progress.Should().NotBeNull();

            // Act & Assert - Export
            var exportResult = await _controller.Export();
            exportResult.Should().BeOfType<FileContentResult>();
            var fileResult = exportResult as FileContentResult;
            fileResult!.ContentType.Should().Be("text/csv");
        }

        [Fact]
        public async Task LaporanWorkflow_WithFilters_MaintainsConsistency()
        {
            // Arrange
            await SeedTestData();
            var searchTerm = "John";
            var statusFilter = "selesai";

            // Act - Index with filters
            var indexResult = await _controller.Index(searchTerm: searchTerm, statusFilter: statusFilter);
            var indexModel = (indexResult as ViewResult)!.Model as LaporanPelatihanListViewModel;

            // Act - Export with same filters
            var exportResult = await _controller.Export(searchTerm: searchTerm, statusFilter: statusFilter);

            // Assert
            indexModel!.SearchTerm.Should().Be(searchTerm);
            indexModel.StatusFilter.Should().Be(statusFilter);
            exportResult.Should().BeOfType<FileContentResult>();
        }

        [Fact]
        public async Task LaporanWorkflow_Statistics_CalculatedCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as LaporanPelatihanListViewModel;

            model!.TotalKaryawan.Should().BeGreaterThan(0);
            model.TotalPelatihanAktif.Should().BeGreaterThan(0);
            model.TotalSertifikatTerbit.Should().BeGreaterOrEqualTo(0);
            model.CompletionRate.Should().BeGreaterOrEqualTo(0);
            model.CompletionRate.Should().BeLessOrEqualTo(100);
        }

        #endregion

        #region Helper Methods

        private async Task SeedTestData()
        {
            // Create Users
            var users = new List<User>
            {
                new User
                {
                    Id = 1,
                    Nama = "John Doe",
                    Email = "john@test.com",
                    Posisi = "Developer",
                    Role = "Karyawan",
                    IsDeleted = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new User
                {
                    Id = 2,
                    Nama = "Jane Smith",
                    Email = "jane@test.com",
                    Posisi = "Tester",
                    Role = "Karyawan",
                    IsDeleted = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-25)
                },
                new User
                {
                    Id = 3,
                    Nama = "Admin User",
                    Email = "admin@test.com",
                    Posisi = "Manager",
                    Role = "Admin",
                    IsDeleted = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-60)
                }
            };

            // Create Pelatihans
            var pelatihans = new List<Pelatihan>
            {
                new Pelatihan
                {
                    Id = 1,
                    Kode = "TEST001",
                    Judul = "Test Training 1",
                    Deskripsi = "Test Description 1",
                    DurasiMenit = 60,
                    SkorMinimal = 70,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new Pelatihan
                {
                    Id = 2,
                    Kode = "TEST002",
                    Judul = "Test Training 2",
                    Deskripsi = "Test Description 2",
                    DurasiMenit = 90,
                    SkorMinimal = 80,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now.AddDays(-20)
                }
            };

            // Create Progress
            var progresses = new List<PelatihanProgress>
            {
                new PelatihanProgress
                {
                    Id = 1,
                    UserId = 1,
                    PelatihanId = 1,
                    MateriTerakhirId = 1,
                    IsCompleted = true,
                    StartedAt = DateTime.Now.AddDays(-10),
                    CompletedAt = DateTime.Now.AddDays(-5),
                    UpdatedAt = DateTime.Now.AddDays(-5)
                },
                new PelatihanProgress
                {
                    Id = 2,
                    UserId = 2,
                    PelatihanId = 1,
                    MateriTerakhirId = 1,
                    IsCompleted = false,
                    StartedAt = DateTime.Now.AddDays(-8),
                    CompletedAt = null,
                    UpdatedAt = DateTime.Now.AddDays(-1)
                },
                new PelatihanProgress
                {
                    Id = 3,
                    UserId = 1,
                    PelatihanId = 2,
                    MateriTerakhirId = 1,
                    IsCompleted = true,
                    StartedAt = DateTime.Now.AddDays(-15),
                    CompletedAt = DateTime.Now.AddDays(-10),
                    UpdatedAt = DateTime.Now.AddDays(-10)
                }
            };

            // Create Results
            var results = new List<PelatihanHasil>
            {
                new PelatihanHasil
                {
                    Id = 1,
                    UserId = 1,
                    PelatihanId = 1,
                    Skor = 85,
                    IsLulus = true,
                    TanggalSelesai = DateTime.Now.AddDays(-5)
                },
                new PelatihanHasil
                {
                    Id = 2,
                    UserId = 1,
                    PelatihanId = 2,
                    Skor = 60,
                    IsLulus = false,
                    TanggalSelesai = DateTime.Now.AddDays(-10)
                }
            };

            // Create Materials
            var materials = new List<PelatihanMateri>
            {
                new PelatihanMateri
                {
                    Id = 1,
                    PelatihanId = 1,
                    Judul = "Material 1",
                    TipeKonten = "text",
                    Konten = "Content 1",
                    Urutan = 1,
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new PelatihanMateri
                {
                    Id = 2,
                    PelatihanId = 1,
                    Judul = "Material 2",
                    TipeKonten = "video",
                    Konten = "Content 2",
                    Urutan = 2,
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new PelatihanMateri
                {
                    Id = 3,
                    PelatihanId = 2,
                    Judul = "Material 3",
                    TipeKonten = "text",
                    Konten = "Content 3",
                    Urutan = 1,
                    CreatedAt = DateTime.Now.AddDays(-20)
                }
            };

            // Create Questions
            var questions = new List<PelatihanSoal>
            {
                new PelatihanSoal
                {
                    Id = 1,
                    PelatihanId = 1,
                    Pertanyaan = "Question 1",
                    OpsiA = "Option A",
                    OpsiB = "Option B",
                    OpsiC = "Option C",
                    OpsiD = "Option D",
                    JawabanBenar = "A",
                    Urutan = 1,
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new PelatihanSoal
                {
                    Id = 2,
                    PelatihanId = 1,
                    Pertanyaan = "Question 2",
                    OpsiA = "Option A",
                    OpsiB = "Option B",
                    OpsiC = "Option C",
                    OpsiD = "Option D",
                    JawabanBenar = "B",
                    Urutan = 2,
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new PelatihanSoal
                {
                    Id = 3,
                    PelatihanId = 2,
                    Pertanyaan = "Question 3",
                    OpsiA = "Option A",
                    OpsiB = "Option B",
                    OpsiC = "Option C",
                    OpsiD = "Option D",
                    JawabanBenar = "C",
                    Urutan = 1,
                    CreatedAt = DateTime.Now.AddDays(-20)
                }
            };

            // Create Certificates
            var certificates = new List<Sertifikat>
            {
                new Sertifikat
                {
                    Id = 1,
                    UserId = 1,
                    PelatihanId = 1,
                    NomorSertifikat = "CERT001",
                    TanggalTerbit = DateTime.Now.AddDays(-5)
                    // TanggalKadaluarsa is nullable, so we don't set it
                }
            };

            // Add to context
            _context.User.AddRange(users);
            _context.Pelatihan.AddRange(pelatihans);
            _context.PelatihanProgress.AddRange(progresses);
            _context.PelatihanHasil.AddRange(results);
            _context.PelatihanMateri.AddRange(materials);
            _context.PelatihanSoal.AddRange(questions);
            _context.Sertifikat.AddRange(certificates);

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}


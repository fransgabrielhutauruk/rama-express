using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using RamaExpress.Areas.Karyawan.Controllers;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using RamaExpress.Areas.Karyawan.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Routing;

namespace RamaExpress.Tests.Controllers.Karyawan
{
    // Create a testable version of the controller
    public class TestablePelatihanController : PelatihanController
    {
        private int? _testUserId;

        public TestablePelatihanController(RamaExpressAppContext context, ILogger<PelatihanController> logger)
            : base(context, logger)
        {
        }

        public void SetTestUserId(int? userId)
        {
            _testUserId = userId;
        }

        protected override int? GetCurrentUserId()
        {
            return _testUserId;
        }
    }

    public class PelatihanControllerKaryawanTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly Mock<ILogger<PelatihanController>> _mockLogger;
        private readonly TestablePelatihanController _controller; // ✅ Fixed: Use TestablePelatihanController
        private readonly Mock<HttpContext> _mockHttpContext;

        public PelatihanControllerKaryawanTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);

            // Setup mocks
            _mockLogger = new Mock<ILogger<PelatihanController>>();
            _mockHttpContext = new Mock<HttpContext>();

            // ✅ Fixed: Create testable controller instance
            _controller = new TestablePelatihanController(_context, _mockLogger.Object);

            // Setup HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                _mockHttpContext.Object,
                Mock.Of<ITempDataProvider>());

            // Setup URL helper for JSON responses
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://localhost/test");
            _controller.Url = mockUrlHelper.Object;
        }

        public void Dispose()
        {
            _context.Dispose();
            _controller?.Dispose();
        }

        #region Index Tests

        [Fact]
        public async Task Index_WithValidUser_ReturnsViewWithCorrectViewModel()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<KaryawanPelatihanViewModel>();

            var model = viewResult.Model as KaryawanPelatihanViewModel;
            model!.AvailableTrainings.Should().NotBeEmpty();
            model.TotalAvailable.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Index_WithUnauthenticatedUser_RedirectsToLogin()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("User");
            redirectResult.RouteValues!["area"].Should().Be("");
        }

        [Fact]
        public async Task Index_WithNonExistentUser_RedirectsToLogin()
        {
            // Arrange
            SetupAuthenticatedUser(999); // Non-existent user

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("User");
        }

        #endregion

        #region Detail Tests

        [Fact]
        public async Task Detail_WithValidTrainingAndAccess_ReturnsViewWithCorrectModel()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Detail(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<PelatihanDetailViewModel>();

            var model = viewResult.Model as PelatihanDetailViewModel;
            model!.Pelatihan.Should().NotBeNull();
            model.Pelatihan.Id.Should().Be(1);
            model.CanStart.Should().BeTrue();
        }

        [Fact]
        public async Task Detail_WithUnauthenticatedUser_RedirectsToLogin()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.Detail(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
        }

        [Fact]
        public async Task Detail_WithNonExistentTraining_RedirectsToIndexWithError()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Detail(999);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Pelatihan tidak ditemukan atau tidak aktif.");
        }

        [Fact]
        public async Task Detail_WithNoAccess_RedirectsToIndexWithError()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(2); // User with different position

            // Act
            var result = await _controller.Detail(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Anda tidak memiliki akses ke pelatihan ini.");
        }

        [Fact]
        public async Task Detail_WithExistingProgress_ShowsCorrectTrainingState()
        {
            // Arrange
            await SeedTestDataWithProgress();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Detail(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as PelatihanDetailViewModel;

            model!.CanStart.Should().BeFalse();
            model.CanContinue.Should().BeTrue();
            model.Progress.Should().NotBeNull();
        }

        #endregion

        #region Mulai (Start Training) Tests

        [Fact]
        public async Task Mulai_WithValidTraining_CreatesProgressAndRedirectsToMaterial()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Mulai(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Materi");

            // Verify progress was created
            var progress = await _context.PelatihanProgress
                .FirstOrDefaultAsync(p => p.UserId == 1 && p.PelatihanId == 1);
            progress.Should().NotBeNull();
            progress!.IsCompleted.Should().BeFalse();
            _controller.TempData["SuccessMessage"].Should().Be("Pelatihan berhasil dimulai!");
        }

        [Fact]
        public async Task Mulai_WithExistingProgress_RedirectsToCurrentMaterial()
        {
            // Arrange
            await SeedTestDataWithProgress();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Mulai(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Materi");
            redirectResult.RouteValues!["materiId"].Should().Be(1);
        }

        [Fact]
        public async Task Mulai_WithUnauthenticatedUser_RedirectsToLogin()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.Mulai(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
        }

        [Fact]
        public async Task Mulai_WithNonExistentTraining_RedirectsToIndexWithError()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Mulai(999);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Pelatihan tidak ditemukan atau tidak aktif.");
        }

        [Fact]
        public async Task Mulai_WithNoMaterials_RedirectsToDetailWithError()
        {
            // Arrange
            await SeedTestDataWithoutMaterials();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Mulai(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Detail");
            _controller.TempData["ErrorMessage"].Should().Be("Pelatihan tidak memiliki materi yang tersedia.");
        }

        #endregion

        #region Materi Tests

        [Fact]
        public async Task Materi_WithValidAccess_ReturnsViewWithMaterial()
        {
            // Arrange
            await SeedTestDataWithProgress();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Materi(1, 1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<MateriViewModel>();

            var model = viewResult.Model as MateriViewModel;
            model!.CurrentMateri.Should().NotBeNull();
            model.CurrentMateri.Id.Should().Be(1);
            model.Pelatihan.Should().NotBeNull();
        }

        [Fact]
        public async Task Materi_WithUnauthenticatedUser_RedirectsToLogin()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.Materi(1, 1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
        }

        [Fact]
        public async Task Materi_WithNonExistentTraining_RedirectsToIndexWithError()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Materi(999, 1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Pelatihan tidak ditemukan atau tidak aktif.");
        }

        [Fact]
        public async Task Materi_WithNonExistentMaterial_RedirectsToDetailWithError()
        {
            // Arrange
            await SeedTestDataWithProgress();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Materi(1, 999);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Detail");
            _controller.TempData["ErrorMessage"].Should().Be("Materi tidak ditemukan.");
        }

        [Fact]
        public async Task Materi_WithNoProgress_RedirectsToDetailWithError()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Materi(1, 1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Detail");
            _controller.TempData["ErrorMessage"].Should().Be("Progress pelatihan tidak ditemukan. Silakan mulai pelatihan terlebih dahulu.");
        }

        [Fact]
        public async Task Materi_WithJumpingAhead_RedirectsToCurrentMaterial()
        {
            // Arrange
            await SeedTestDataWithProgress();
            SetupAuthenticatedUser(1);

            // Act - Try to access material 3 when user is only on material 1
            var result = await _controller.Materi(1, 3);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Materi");
            redirectResult.RouteValues!["materiId"].Should().Be(1); // Redirect to current material
            _controller.TempData["ErrorMessage"].ToString().Should().Contain("Anda harus menyelesaikan materi secara berurutan");
        }

        [Fact]
        public async Task Materi_WithAccessingNextMaterial_UpdatesProgress()
        {
            // Arrange
            await SeedTestDataWithProgress();
            SetupAuthenticatedUser(1);

            // Act - Access next material (material 2)
            var result = await _controller.Materi(1, 2);

            // Assert
            result.Should().BeOfType<ViewResult>();

            // Verify progress was updated
            var progress = await _context.PelatihanProgress
                .FirstOrDefaultAsync(p => p.UserId == 1 && p.PelatihanId == 1);
            progress!.MateriTerakhirId.Should().Be(2);
        }

        #endregion

        #region CompleteMateri Tests

        [Fact]
        public async Task CompleteMateri_WithValidMaterial_ReturnsJsonSuccess()
        {
            // Arrange
            await SeedTestDataWithProgress();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.CompleteMateri(1, 1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            // Parse the JSON response
            var responseJson = JsonSerializer.Serialize(jsonResult!.Value);
            responseJson.Should().Contain("\"success\":true");
            responseJson.Should().Contain("\"isCompleted\":false"); // Not the last material
        }

        [Fact]
        public async Task CompleteMateri_WithLastMaterial_MarksTrainingComplete()
        {
            // Arrange
            await SeedTestDataWithProgressOnLastMaterial();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.CompleteMateri(1, 3); // Last material

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var responseJson = JsonSerializer.Serialize(jsonResult!.Value);
            responseJson.Should().Contain("\"success\":true");
            responseJson.Should().Contain("\"isCompleted\":true");

            // Verify training is marked as completed
            var progress = await _context.PelatihanProgress
                .FirstOrDefaultAsync(p => p.UserId == 1 && p.PelatihanId == 1);
            progress!.IsCompleted.Should().BeTrue();
            progress.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task CompleteMateri_WithUnauthenticatedUser_ReturnsJsonError()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.CompleteMateri(1, 1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var responseJson = JsonSerializer.Serialize(jsonResult!.Value);
            responseJson.Should().Contain("\"success\":false");
            responseJson.Should().Contain("Session expired");
        }

        [Fact]
        public async Task CompleteMateri_WithNoProgress_ReturnsJsonError()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.CompleteMateri(1, 1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var responseJson = JsonSerializer.Serialize(jsonResult!.Value);
            responseJson.Should().Contain("\"success\":false");
            responseJson.Should().Contain("Progress tidak ditemukan");
        }

        #endregion

        #region Ujian Tests

        [Fact]
        public async Task Ujian_WithCompletedMaterials_ReturnsViewWithExam()
        {
            // Arrange
            await SeedTestDataWithCompletedMaterials();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Ujian(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<UjianViewModel>();

            var model = viewResult.Model as UjianViewModel;
            model!.Pelatihan.Should().NotBeNull();
            model.Questions.Should().NotBeEmpty();
            model.TimeLimit.Should().Be(120 * 60); // 2 hours in seconds
        }

        [Fact]
        public async Task Ujian_WithUnauthenticatedUser_RedirectsToLogin()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.Ujian(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
        }

        [Fact]
        public async Task Ujian_WithIncompleteMaterials_RedirectsToDetailWithError()
        {
            // Arrange
            await SeedTestDataWithProgress();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Ujian(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Detail");
            _controller.TempData["ErrorMessage"].ToString().Should().Contain("Anda harus menyelesaikan semua materi terlebih dahulu");
        }

        [Fact]
        public async Task Ujian_WithAlreadyTakenExam_RedirectsToHasilUjian()
        {
            // Arrange
            await SeedTestDataWithExamResult();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Ujian(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("HasilUjian");
            _controller.TempData["ErrorMessage"].Should().Be("Anda sudah mengikuti ujian untuk pelatihan ini.");
        }

        [Fact]
        public async Task Ujian_WithNoQuestions_RedirectsToDetailWithError()
        {
            // Arrange
            await SeedTestDataWithoutQuestions();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Ujian(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Detail");
            _controller.TempData["ErrorMessage"].Should().Be("Ujian tidak tersedia untuk pelatihan ini.");
        }

        #endregion

        #region SubmitUjian Tests

        [Fact]
        public async Task SubmitUjian_WithCorrectAnswers_ReturnsJsonSuccessWithPassingScore()
        {
            // Arrange
            await SeedTestDataWithCompletedMaterials();
            SetupAuthenticatedUser(1);
            var answers = new List<string> { "A", "B", "A" }; // All correct answers

            // Act
            var result = await _controller.SubmitUjian(1, answers);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var responseJson = JsonSerializer.Serialize(jsonResult!.Value);
            responseJson.Should().Contain("\"success\":true");
            responseJson.Should().Contain("\"score\":100");
            responseJson.Should().Contain("\"isLulus\":true");

            // Verify result was saved
            var hasil = await _context.PelatihanHasil
                .FirstOrDefaultAsync(h => h.UserId == 1 && h.PelatihanId == 1);
            hasil.Should().NotBeNull();
            hasil!.Skor.Should().Be(100);
            hasil.IsLulus.Should().BeTrue();
        }

        [Fact]
        public async Task SubmitUjian_WithIncorrectAnswers_ReturnsJsonSuccessWithFailingScore()
        {
            // Arrange
            await SeedTestDataWithCompletedMaterials();
            SetupAuthenticatedUser(1);
            var answers = new List<string> { "D", "D", "D" }; // All incorrect answers

            // Act
            var result = await _controller.SubmitUjian(1, answers);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var responseJson = JsonSerializer.Serialize(jsonResult!.Value);
            responseJson.Should().Contain("\"success\":true");
            responseJson.Should().Contain("\"score\":0");
            responseJson.Should().Contain("\"isLulus\":false");

            var hasil = await _context.PelatihanHasil
                .FirstOrDefaultAsync(h => h.UserId == 1 && h.PelatihanId == 1);
            hasil!.Skor.Should().Be(0);
            hasil.IsLulus.Should().BeFalse();
        }

        [Fact]
        public async Task SubmitUjian_WithPassingScore_GeneratesCertificate()
        {
            // Arrange
            await SeedTestDataWithCertificateConfig();
            SetupAuthenticatedUser(1);
            var answers = new List<string> { "A", "B", "A" }; // All correct answers

            // Act
            var result = await _controller.SubmitUjian(1, answers);

            // Assert
            result.Should().BeOfType<JsonResult>();

            // Verify certificate was generated
            var certificate = await _context.Sertifikat
                .FirstOrDefaultAsync(s => s.UserId == 1 && s.PelatihanId == 1);
            certificate.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitUjian_WithUnauthenticatedUser_ReturnsJsonError()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.SubmitUjian(1, new List<string>());

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var responseJson = JsonSerializer.Serialize(jsonResult!.Value);
            responseJson.Should().Contain("\"success\":false");
            responseJson.Should().Contain("Session expired");
        }

        [Fact]
        public async Task SubmitUjian_WithAlreadySubmitted_ReturnsJsonError()
        {
            // Arrange
            await SeedTestDataWithExamResult();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.SubmitUjian(1, new List<string>());

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            var responseJson = JsonSerializer.Serialize(jsonResult!.Value);
            responseJson.Should().Contain("\"success\":false");
            responseJson.Should().Contain("Ujian sudah pernah dikerjakan");
        }

        #endregion

        #region HasilUjian Tests

        [Fact]
        public async Task HasilUjian_WithValidResult_ReturnsViewWithResult()
        {
            // Arrange
            await SeedTestDataWithExamResult();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.HasilUjian(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<HasilUjianViewModel>();

            var model = viewResult.Model as HasilUjianViewModel;
            model!.Hasil.Should().NotBeNull();
            model.Hasil.PelatihanId.Should().Be(1);
        }

        [Fact]
        public async Task HasilUjian_WithUnauthenticatedUser_RedirectsToLogin()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.HasilUjian(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
        }

        [Fact]
        public async Task HasilUjian_WithNoResult_RedirectsToDetailWithError()
        {
            // Arrange
            await SeedTestData();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.HasilUjian(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Detail");
            _controller.TempData["ErrorMessage"].Should().Be("Hasil ujian tidak ditemukan.");
        }

        #endregion

        #region Certificate Tests

        [Fact]
        public async Task Sertifikat_WithValidUser_ReturnsViewWithCertificates()
        {
            // Arrange
            await SeedTestDataWithCertificate();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.Sertifikat();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<List<Sertifikat>>();

            var model = viewResult.Model as List<Sertifikat>;
            model!.Should().NotBeEmpty();
            model.Should().AllSatisfy(s => s.UserId.Should().Be(1));
        }

        [Fact]
        public async Task SertifikatDetail_WithValidCertificate_ReturnsViewWithCertificate()
        {
            // Arrange
            await SeedTestDataWithCertificate();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.SertifikatDetail(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<Sertifikat>();

            var model = viewResult.Model as Sertifikat;
            model!.Id.Should().Be(1);
            model.UserId.Should().Be(1);
        }

        [Fact]
        public async Task SertifikatDetail_WithUnauthorizedCertificate_RedirectsToSertifikatWithError()
        {
            // Arrange
            await SeedTestDataWithCertificate();
            SetupAuthenticatedUser(2); // Different user

            // Act
            var result = await _controller.SertifikatDetail(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Sertifikat");
            _controller.TempData["ErrorMessage"].Should().Be("Sertifikat tidak ditemukan atau bukan milik Anda.");
        }

        [Fact]
        public async Task DownloadSertifikat_WithValidCertificate_ReturnsFileResult()
        {
            // Arrange
            await SeedTestDataWithCertificate();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.DownloadSertifikat(1);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("application/pdf");
            fileResult.FileDownloadName.Should().StartWith("Sertifikat_");
        }

        [Fact]
        public async Task PreviewSertifikat_WithValidCertificate_ReturnsFileResult()
        {
            // Arrange
            await SeedTestDataWithCertificate();
            SetupAuthenticatedUser(1);

            // Act
            var result = await _controller.PreviewSertifikat(1);

            // Assert
            result.Should().BeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult!.ContentType.Should().Be("application/pdf");
        }

        #endregion

        #region Helper Methods

        private void SetupAuthenticatedUser(int userId)
        {
            _controller.SetTestUserId(userId); // ✅ Now this will work
        }

        private void SetupUnauthenticatedUser()
        {
            _controller.SetTestUserId(null); // ✅ Now this will work
        }

        private async Task SeedTestData()
        {
            // Create users
            var users = new List<User>
            {
                new() { Id = 1, Nama = "John Doe", Email = "john@test.com", Posisi = "Developer", IsActive = true },
                new() { Id = 2, Nama = "Jane Smith", Email = "jane@test.com", Posisi = "Tester", IsActive = true }
            };

            // Create positions
            var positions = new List<Posisi>
            {
                new() { Id = 1, Name = "Developer", IsDeleted = false },
                new() { Id = 2, Name = "Tester", IsDeleted = false }
            };

            // Create training
            var training = new Pelatihan
            {
                Id = 1,
                Kode = "TEST001",
                Judul = "Test Training",
                Deskripsi = "Test Description",
                DurasiMenit = 60,
                SkorMinimal = 70,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            // Create materials
            var materials = new List<PelatihanMateri>
            {
                new() { Id = 1, PelatihanId = 1, Judul = "Material 1", TipeKonten = "text", Konten = "Content 1", Urutan = 1 },
                new() { Id = 2, PelatihanId = 1, Judul = "Material 2", TipeKonten = "text", Konten = "Content 2", Urutan = 2 },
                new() { Id = 3, PelatihanId = 1, Judul = "Material 3", TipeKonten = "text", Konten = "Content 3", Urutan = 3 }
            };

            // Create questions
            var questions = new List<PelatihanSoal>
            {
                new() { Id = 1, PelatihanId = 1, Pertanyaan = "Question 1", OpsiA = "A1", OpsiB = "B1", OpsiC = "C1", OpsiD = "D1", JawabanBenar = "A", Urutan = 1 },
                new() { Id = 2, PelatihanId = 1, Pertanyaan = "Question 2", OpsiA = "A2", OpsiB = "B2", OpsiC = "C2", OpsiD = "D2", JawabanBenar = "B", Urutan = 2 },
                new() { Id = 3, PelatihanId = 1, Pertanyaan = "Question 3", OpsiA = "A3", OpsiB = "B3", OpsiC = "C3", OpsiD = "D3", JawabanBenar = "A", Urutan = 3 }
            };

            // Create position assignments
            var positionAssignments = new List<PelatihanPosisi>
            {
                new() { PelatihanId = 1, PosisiId = 1 }
            };

            _context.User.AddRange(users);
            _context.Posisi.AddRange(positions);
            _context.Pelatihan.Add(training);
            _context.PelatihanMateri.AddRange(materials);
            _context.PelatihanSoal.AddRange(questions);
            _context.PelatihanPosisi.AddRange(positionAssignments);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithProgress()
        {
            await SeedTestData();

            var progress = new PelatihanProgress
            {
                Id = 1,
                UserId = 1,
                PelatihanId = 1,
                MateriTerakhirId = 1,
                IsCompleted = false,
                StartedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.PelatihanProgress.Add(progress);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithProgressOnLastMaterial()
        {
            await SeedTestData();

            var progress = new PelatihanProgress
            {
                Id = 1,
                UserId = 1,
                PelatihanId = 1,
                MateriTerakhirId = 3, // Last material
                IsCompleted = false,
                StartedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.PelatihanProgress.Add(progress);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithCompletedMaterials()
        {
            await SeedTestData();

            var progress = new PelatihanProgress
            {
                Id = 1,
                UserId = 1,
                PelatihanId = 1,
                MateriTerakhirId = 3,
                IsCompleted = true,
                StartedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.PelatihanProgress.Add(progress);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithExamResult()
        {
            await SeedTestDataWithCompletedMaterials();

            var result = new PelatihanHasil
            {
                Id = 1,
                UserId = 1,
                PelatihanId = 1,
                Skor = 85,
                IsLulus = true,
                TanggalSelesai = DateTime.Now
            };

            _context.PelatihanHasil.Add(result);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithoutMaterials()
        {
            // Create basic data without materials
            var users = new List<User>
            {
                new() { Id = 1, Nama = "John Doe", Email = "john@test.com", Posisi = "Developer", IsActive = true }
            };

            var positions = new List<Posisi>
            {
                new() { Id = 1, Name = "Developer", IsDeleted = false }
            };

            var training = new Pelatihan
            {
                Id = 1,
                Kode = "TEST001",
                Judul = "Test Training",
                DurasiMenit = 60,
                SkorMinimal = 70,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            var positionAssignments = new List<PelatihanPosisi>
            {
                new() { PelatihanId = 1, PosisiId = 1 }
            };

            _context.User.AddRange(users);
            _context.Posisi.AddRange(positions);
            _context.Pelatihan.Add(training);
            _context.PelatihanPosisi.AddRange(positionAssignments);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithoutQuestions()
        {
            await SeedTestDataWithCompletedMaterials();

            // Remove questions
            var questions = await _context.PelatihanSoal.Where(q => q.PelatihanId == 1).ToListAsync();
            _context.PelatihanSoal.RemoveRange(questions);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithCertificateConfig()
        {
            await SeedTestDataWithCompletedMaterials();

            var certificateConfig = new PelatihanSertifikat
            {
                Id = 1,
                PelatihanId = 1,
                TemplateName = "Test Certificate",
                CertificateNumberFormat = "CERT/{YEAR}/{MONTH}/{INCREMENT}",
                ExpirationType = "never",
                IsSertifikatActive = true
            };

            _context.PelatihanSertifikat.Add(certificateConfig);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithCertificate()
        {
            await SeedTestDataWithExamResult();

            var certificate = new Sertifikat
            {
                Id = 1,
                UserId = 1,
                PelatihanId = 1,
                NomorSertifikat = "CERT/2024/01/0001",
                TanggalTerbit = DateTime.Now,
                TanggalKadaluarsa = DateTime.MaxValue
            };

            _context.Sertifikat.Add(certificate);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}

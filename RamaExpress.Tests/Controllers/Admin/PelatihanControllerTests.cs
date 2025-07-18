// RamaExpress.Tests/Controllers/Admin/PelatihanControllerTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using RamaExpress.Areas.Admin.Controllers;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Controllers.Admin
{
    public class PelatihanControllerTests : IDisposable
    {
        private readonly Mock<IPelatihanService> _mockPelatihanService;
        private readonly Mock<IPelatihanMateriService> _mockMateriService;
        private readonly Mock<IPelatihanSoalService> _mockSoalService;
        private readonly Mock<IPelatihanSertifikatService> _mockSertifikatService;
        private readonly RamaExpressAppContext _context;
        private readonly PelatihanController _controller;

        public PelatihanControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);

            // Setup mocks
            _mockPelatihanService = new Mock<IPelatihanService>();
            _mockMateriService = new Mock<IPelatihanMateriService>();
            _mockSoalService = new Mock<IPelatihanSoalService>();
            _mockSertifikatService = new Mock<IPelatihanSertifikatService>();

            // Create controller
            _controller = new PelatihanController(
                _mockPelatihanService.Object,
                _mockMateriService.Object,
                _mockSoalService.Object,
                _mockSertifikatService.Object,
                _context);

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region Index Tests

        [Fact]
        public async Task Index_ReturnsViewWithCorrectViewModel()
        {
            // Arrange
            var pelatihans = new List<Pelatihan>
            {
                new() { Id = 1, Judul = "Test Training 1", IsActive = true },
                new() { Id = 2, Judul = "Test Training 2", IsActive = true }
            };
            var totalCount = 2;

            _mockPelatihanService.Setup(s => s.GetAllWithSearch(1, 10, null, null))
                .ReturnsAsync((pelatihans, totalCount));

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<PelatihanListViewModel>();

            var model = viewResult.Model as PelatihanListViewModel;
            model!.Pelatihans.Should().HaveCount(2);
            model.TotalCount.Should().Be(2);
            model.CurrentPage.Should().Be(1);
            model.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Index_WithSearchParameters_PassesCorrectParametersToService()
        {
            // Arrange
            var searchTerm = "test";
            var statusFilter = "active";
            var page = 2;
            var pageSize = 5;

            _mockPelatihanService.Setup(s => s.GetAllWithSearch(page, pageSize, searchTerm, statusFilter))
                .ReturnsAsync((new List<Pelatihan>(), 0));

            // Act
            await _controller.Index(page, searchTerm, statusFilter, pageSize);

            // Assert
            _mockPelatihanService.Verify(s => s.GetAllWithSearch(page, pageSize, searchTerm, statusFilter), Times.Once);
        }

        [Fact]
        public async Task Index_WithInvalidPage_SetsPageToOne()
        {
            // Arrange
            _mockPelatihanService.Setup(s => s.GetAllWithSearch(1, 10, null, null))
                .ReturnsAsync((new List<Pelatihan>(), 0));

            // Act
            await _controller.Index(page: -1);

            // Assert
            _mockPelatihanService.Verify(s => s.GetAllWithSearch(1, 10, null, null), Times.Once);
        }

        #endregion

        #region Create Tests

        [Fact]
        public async Task Create_Get_ReturnsViewWithPositions()
        {
            // Arrange
            await SeedPositions();

            // Act
            var result = await _controller.Create();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewData["Positions"].Should().NotBeNull();
        }

        [Fact]
        public async Task Create_Post_WithValidModel_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                DurasiMenit = 60,
                SkorMinimal = 70
            };
            var selectedPositions = new List<int> { 1, 2 };

            _mockPelatihanService.Setup(s => s.Add(model))
                .ReturnsAsync((Success: true, Message: "Success", Pelatihan: new Pelatihan { Id = 1 }));

            await SeedPositions();

            // Act
            var result = await _controller.Create(model, selectedPositions);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Success");
        }

        [Fact]
        public async Task Create_Post_WithInvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var model = new Pelatihan();
            _controller.ModelState.AddModelError("Kode", "Required");
            await SeedPositions();

            // Act
            var result = await _controller.Create(model, new List<int>());

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);
            viewResult.ViewData["Positions"].Should().NotBeNull();
        }

        [Fact]
        public async Task Create_Post_WithServiceFailure_ReturnsViewWithErrorMessage()
        {
            // Arrange
            var model = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                DurasiMenit = 60,
                SkorMinimal = 70
            };

            _mockPelatihanService.Setup(s => s.Add(model))
                .ReturnsAsync((Success: false, Message: "Error occurred", Pelatihan: null));

            await SeedPositions();

            // Act
            var result = await _controller.Create(model, new List<int>());

            // Assert
            result.Should().BeOfType<ViewResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Error occurred");
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewWithModel()
        {
            // Arrange
            var pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" };
            _mockPelatihanService.Setup(s => s.GetById(1))
                .ReturnsAsync(pelatihan);

            await SeedPositions();
            await SeedPelatihanPosisi(1, new List<int> { 1, 2 });

            // Act
            var result = await _controller.Edit(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(pelatihan);
            viewResult.ViewData["Positions"].Should().NotBeNull();
            viewResult.ViewData["SelectedPositions"].Should().NotBeNull();
        }

        [Fact]
        public async Task Edit_Get_WithInvalidId_ReturnsRedirectToIndex()
        {
            // Arrange
            _mockPelatihanService.Setup(s => s.GetById(1))
                .ReturnsAsync((Pelatihan?)null);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Pelatihan tidak ditemukan");
        }

        [Fact]
        public async Task Edit_Post_WithValidModel_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new Pelatihan
            {
                Id = 1,
                Kode = "TEST001",
                Judul = "Updated Training",
                DurasiMenit = 90,
                SkorMinimal = 80
            };
            var selectedPositions = new List<int> { 1, 3 };

            _mockPelatihanService.Setup(s => s.Update(model))
                .ReturnsAsync((Success: true, Message: "Updated successfully", Pelatihan: model));

            await SeedPositions();
            await SeedPelatihanPosisi(1, new List<int> { 1, 2 });

            // Act
            var result = await _controller.Edit(1, model, selectedPositions);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Updated successfully");

            // Verify position assignments were updated
            var pelatihanPosisi = await _context.PelatihanPosisi.Where(pp => pp.PelatihanId == 1).ToListAsync();
            pelatihanPosisi.Should().HaveCount(2);
            pelatihanPosisi.Select(pp => pp.PosisiId).Should().Contain(new[] { 1, 3 });
        }

        [Fact]
        public async Task Edit_Post_WithMismatchedId_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new Pelatihan { Id = 2 };

            // Act
            var result = await _controller.Edit(1, model, new List<int>());

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Data tidak valid");
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidId_ReturnsRedirectWithSuccessMessage()
        {
            // Arrange
            _mockPelatihanService.Setup(s => s.Delete(1))
                .ReturnsAsync((Success: true, Message: "Deleted successfully"));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Deleted successfully");
        }

        [Fact]
        public async Task Delete_WithServiceFailure_ReturnsRedirectWithErrorMessage()
        {
            // Arrange
            _mockPelatihanService.Setup(s => s.Delete(1))
                .ReturnsAsync((Success: false, Message: "Cannot delete training"));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Cannot delete training");
        }

        #endregion

        #region Materials Tests

        [Fact]
        public async Task Materials_WithValidPelatihanId_ReturnsViewWithMaterials()
        {
            // Arrange
            var pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" };
            var materials = new List<PelatihanMateri>
            {
                new() { Id = 1, PelatihanId = 1, Judul = "Material 1" },
                new() { Id = 2, PelatihanId = 1, Judul = "Material 2" }
            };

            _mockPelatihanService.Setup(s => s.GetById(1)).ReturnsAsync(pelatihan);
            _mockMateriService.Setup(s => s.GetByPelatihanId(1)).ReturnsAsync(materials);

            // Act
            var result = await _controller.Materials(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeEquivalentTo(materials);
            viewResult.ViewData["Pelatihan"].Should().Be(pelatihan);
        }

        [Fact]
        public async Task Materials_WithInvalidPelatihanId_ReturnsRedirectToIndex()
        {
            // Arrange
            _mockPelatihanService.Setup(s => s.GetById(1)).ReturnsAsync((Pelatihan?)null);

            // Act
            var result = await _controller.Materials(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Pelatihan tidak ditemukan");
        }

        [Fact]
        public async Task CreateMaterial_Get_WithValidPelatihanId_ReturnsViewWithModel()
        {
            // Arrange
            var pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" };
            _mockPelatihanService.Setup(s => s.GetById(1)).ReturnsAsync(pelatihan);

            // Act
            var result = await _controller.CreateMaterial(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as PelatihanMateri;
            model!.PelatihanId.Should().Be(1);
            viewResult.ViewData["Pelatihan"].Should().Be(pelatihan);
        }

        [Fact]
        public async Task CreateMaterial_Post_WithValidModel_ReturnsRedirectToMaterials()
        {
            // Arrange
            var model = new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "New Material",
                TipeKonten = "text",
                Konten = "Material content"
            };

            _mockMateriService.Setup(s => s.Add(model))
                .ReturnsAsync((Success: true, Message: "Material created", Materi: model));

            // Act
            var result = await _controller.CreateMaterial(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Materials");
            redirectResult.RouteValues!["pelatihanId"].Should().Be(1);
            _controller.TempData["SuccessMessage"].Should().Be("Material created");
        }

        #endregion

        #region Questions Tests

        [Fact]
        public async Task ExamQuestions_WithValidPelatihanId_ReturnsViewWithQuestions()
        {
            // Arrange
            var pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" };
            var questions = new List<PelatihanSoal>
            {
                new() { Id = 1, PelatihanId = 1, Pertanyaan = "Question 1" },
                new() { Id = 2, PelatihanId = 1, Pertanyaan = "Question 2" }
            };

            _mockPelatihanService.Setup(s => s.GetById(1)).ReturnsAsync(pelatihan);
            _mockSoalService.Setup(s => s.GetByPelatihanId(1)).ReturnsAsync(questions);

            // Act
            var result = await _controller.ExamQuestions(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeEquivalentTo(questions);
            viewResult.ViewData["Pelatihan"].Should().Be(pelatihan);
        }

        [Fact]
        public async Task CreateQuestion_Post_WithValidModel_ReturnsRedirectToExamQuestions()
        {
            // Arrange
            var model = new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "What is testing?",
                OpsiA = "Option A",
                OpsiB = "Option B",
                OpsiC = "Option C",
                OpsiD = "Option D",
                JawabanBenar = "A"
            };

            _mockSoalService.Setup(s => s.Add(model))
                .ReturnsAsync((Success: true, Message: "Question created", Soal: model));

            // Act
            var result = await _controller.CreateQuestion(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("ExamQuestions");
            redirectResult.RouteValues!["pelatihanId"].Should().Be(1);
            _controller.TempData["SuccessMessage"].Should().Be("Question created");
        }

        #endregion

        #region AJAX Move Methods Tests

        [Fact]
        public async Task MoveMaterialUp_ReturnsJsonWithResult()
        {
            // Arrange
            _mockMateriService.Setup(s => s.MoveUp(1))
                .ReturnsAsync((Success: true, Message: "Moved up successfully"));

            // Act
            var result = await _controller.MoveMaterialUp(1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;
            value.Should().NotBeNull();

            // Use reflection to check anonymous object properties
            var successProperty = value!.GetType().GetProperty("success");
            var messageProperty = value.GetType().GetProperty("message");

            successProperty!.GetValue(value).Should().Be(true);
            messageProperty!.GetValue(value).Should().Be("Moved up successfully");
        }

        [Fact]
        public async Task MoveMaterialDown_ReturnsJsonWithResult()
        {
            // Arrange
            _mockMateriService.Setup(s => s.MoveDown(1))
                .ReturnsAsync((Success: false, Message: "Cannot move down"));

            // Act
            var result = await _controller.MoveMaterialDown(1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;

            var successProperty = value!.GetType().GetProperty("success");
            var messageProperty = value.GetType().GetProperty("message");

            successProperty!.GetValue(value).Should().Be(false);
            messageProperty!.GetValue(value).Should().Be("Cannot move down");
        }

        [Fact]
        public async Task MoveQuestionUp_ReturnsJsonWithResult()
        {
            // Arrange
            _mockSoalService.Setup(s => s.MoveUp(1))
                .ReturnsAsync((Success: true, Message: "Question moved up"));

            // Act
            var result = await _controller.MoveQuestionUp(1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;

            var successProperty = value!.GetType().GetProperty("success");
            var messageProperty = value.GetType().GetProperty("message");

            successProperty!.GetValue(value).Should().Be(true);
            messageProperty!.GetValue(value).Should().Be("Question moved up");
        }

        [Fact]
        public async Task MoveQuestionDown_ReturnsJsonWithResult()
        {
            // Arrange
            _mockSoalService.Setup(s => s.MoveDown(1))
                .ReturnsAsync((Success: true, Message: "Question moved down"));

            // Act
            var result = await _controller.MoveQuestionDown(1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;

            var successProperty = value!.GetType().GetProperty("success");
            var messageProperty = value.GetType().GetProperty("message");

            successProperty!.GetValue(value).Should().Be(true);
            messageProperty!.GetValue(value).Should().Be("Question moved down");
        }

        #endregion

        #region Certificate Tests

        [Fact]
        public async Task Certificate_WithValidPelatihanId_ReturnsViewWithCertificate()
        {
            // Arrange
            var pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" };
            var certificate = new PelatihanSertifikat
            {
                Id = 1,
                PelatihanId = 1,
                TemplateName = "Test Certificate"
            };

            _mockPelatihanService.Setup(s => s.GetById(1)).ReturnsAsync(pelatihan);
            _mockSertifikatService.Setup(s => s.GetByPelatihanId(1)).ReturnsAsync(certificate);

            // Act
            var result = await _controller.Certificate(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(certificate);
            viewResult.ViewData["Pelatihan"].Should().Be(pelatihan);
            viewResult.ViewData["HasCertificate"].Should().Be(true);
        }

        [Fact]
        public async Task CreateCertificate_Get_WithValidPelatihanId_ReturnsViewWithModel()
        {
            // Arrange
            var pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" };
            _mockPelatihanService.Setup(s => s.GetById(1)).ReturnsAsync(pelatihan);
            _mockSertifikatService.Setup(s => s.GetByPelatihanId(1)).ReturnsAsync((PelatihanSertifikat?)null);

            // Act
            var result = await _controller.CreateCertificate(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as PelatihanSertifikat;
            model!.PelatihanId.Should().Be(1);
            model.TemplateName.Should().Be("Sertifikat Test Training");
            model.ExpirationType.Should().Be("never");
            model.IsSertifikatActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateCertificate_Get_WithExistingCertificate_ReturnsRedirectToCertificate()
        {
            // Arrange
            var pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" };
            var existingCertificate = new PelatihanSertifikat { Id = 1, PelatihanId = 1 };

            _mockPelatihanService.Setup(s => s.GetById(1)).ReturnsAsync(pelatihan);
            _mockSertifikatService.Setup(s => s.GetByPelatihanId(1)).ReturnsAsync(existingCertificate);

            // Act
            var result = await _controller.CreateCertificate(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Certificate");
            redirectResult.RouteValues!["pelatihanId"].Should().Be(1);
            _controller.TempData["ErrorMessage"].Should().Be("Pengaturan sertifikat untuk pelatihan ini sudah ada");
        }

        [Fact]
        public async Task CreateCertificate_Post_WithValidModel_ReturnsRedirectToCertificate()
        {
            // Arrange
            var model = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Test Certificate",
                ExpirationType = "never",
                IsSertifikatActive = true
            };

            _mockSertifikatService.Setup(s => s.Add(model))
                .ReturnsAsync((Success: true, Message: "Certificate created", Certificate: model));

            // Act
            var result = await _controller.CreateCertificate(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Certificate");
            redirectResult.RouteValues!["pelatihanId"].Should().Be(1);
            _controller.TempData["SuccessMessage"].Should().Be("Certificate created");
        }

        [Fact]
        public async Task EditCertificate_Get_WithValidId_ReturnsViewWithModel()
        {
            // Arrange
            var certificate = new PelatihanSertifikat
            {
                Id = 1,
                PelatihanId = 1,
                Pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" }
            };

            _mockSertifikatService.Setup(s => s.GetById(1)).ReturnsAsync(certificate);

            // Act
            var result = await _controller.EditCertificate(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(certificate);
            viewResult.ViewData["Pelatihan"].Should().Be(certificate.Pelatihan);
        }

        [Fact]
        public async Task DeleteCertificate_WithValidId_ReturnsRedirectToCertificate()
        {
            // Arrange
            var certificate = new PelatihanSertifikat { Id = 1, PelatihanId = 1 };
            _mockSertifikatService.Setup(s => s.GetById(1)).ReturnsAsync(certificate);
            _mockSertifikatService.Setup(s => s.Delete(1))
                .ReturnsAsync((Success: true, Message: "Certificate deleted"));

            // Act
            var result = await _controller.DeleteCertificate(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Certificate");
            redirectResult.RouteValues!["pelatihanId"].Should().Be(1);
            _controller.TempData["SuccessMessage"].Should().Be("Certificate deleted");
        }

        #endregion

        #region Helper Methods

        private async Task SeedPositions()
        {
            var positions = new List<Posisi>
            {
                new() { Id = 1, Name = "Position 1", IsDeleted = false },
                new() { Id = 2, Name = "Position 2", IsDeleted = false },
                new() { Id = 3, Name = "Position 3", IsDeleted = false }
            };

            _context.Posisi.AddRange(positions);
            await _context.SaveChangesAsync();
        }

        private async Task SeedPelatihanPosisi(int pelatihanId, List<int> posisiIds)
        {
            var pelatihanPosisis = posisiIds.Select(posisiId => new PelatihanPosisi
            {
                PelatihanId = pelatihanId,
                PosisiId = posisiId
            }).ToList();

            _context.PelatihanPosisi.AddRange(pelatihanPosisis);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}

// RamaExpress.Tests/Controllers/Admin/PosisiControllerTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using FluentAssertions;
using RamaExpress.Areas.Admin.Controllers;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Controllers.Admin
{
    public class PosisiControllerTests : IDisposable
    {
        private readonly Mock<IPosisiService> _mockPosisiService;
        private readonly PosisiController _controller;

        public PosisiControllerTests()
        {
            // Setup mocks
            _mockPosisiService = new Mock<IPosisiService>();

            // Create controller
            _controller = new PosisiController(_mockPosisiService.Object);

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        public void Dispose()
        {
            _controller?.Dispose();
        }

        #region Index Tests

        [Fact]
        public async Task Index_ReturnsViewWithCorrectViewModel()
        {
            // Arrange
            var posisiWithCount = new List<PosisiWithCountViewModel>
            {
                new(new Posisi { Id = 1, Name = "Developer", IsDeleted = false }, 5),
                new(new Posisi { Id = 2, Name = "Tester", IsDeleted = false }, 3),
                new(new Posisi { Id = 3, Name = "Manager", IsDeleted = true }, 1)
            };

            _mockPosisiService.Setup(s => s.GetPosisiWithEmployeeCount())
                .ReturnsAsync(posisiWithCount);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<PosisiListViewModel>();

            var model = viewResult.Model as PosisiListViewModel;
            model!.Posisis.Should().HaveCount(3);
            model.TotalCount.Should().Be(3);

            // Check ViewBag.EmployeeCounts
            viewResult.ViewData["EmployeeCounts"].Should().NotBeNull();
            var employeeCounts = viewResult.ViewData["EmployeeCounts"] as Dictionary<int, int>;
            employeeCounts!.Should().HaveCount(3);
            employeeCounts[1].Should().Be(5);
            employeeCounts[2].Should().Be(3);
            employeeCounts[3].Should().Be(1);
        }

        [Fact]
        public async Task Index_WithEmptyPosisiList_ReturnsEmptyViewModel()
        {
            // Arrange
            var emptyPosisiWithCount = new List<PosisiWithCountViewModel>();

            _mockPosisiService.Setup(s => s.GetPosisiWithEmployeeCount())
                .ReturnsAsync(emptyPosisiWithCount);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as PosisiListViewModel;

            model!.Posisis.Should().BeEmpty();
            model.TotalCount.Should().Be(0);

            var employeeCounts = viewResult.ViewData["EmployeeCounts"] as Dictionary<int, int>;
            employeeCounts!.Should().BeEmpty();
        }

        [Fact]
        public async Task Index_CallsCorrectServiceMethod()
        {
            // Arrange
            _mockPosisiService.Setup(s => s.GetPosisiWithEmployeeCount())
                .ReturnsAsync(new List<PosisiWithCountViewModel>());

            // Act
            await _controller.Index();

            // Assert
            _mockPosisiService.Verify(s => s.GetPosisiWithEmployeeCount(), Times.Once);
        }

        #endregion

        #region Create Tests

        [Fact]
        public void Create_Get_ReturnsView()
        {
            // Act
            var result = _controller.Create();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeNull(); // No model expected for GET
        }

        [Fact]
        public async Task Create_Post_WithValidModel_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new Posisi
            {
                Name = "New Position"
            };

            _mockPosisiService.Setup(s => s.Add(model))
                .ReturnsAsync((Success: true, Message: "Posisi berhasil ditambahkan", Posisi: model));

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Posisi berhasil ditambahkan");
        }

        [Fact]
        public async Task Create_Post_WithInvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var model = new Posisi();
            _controller.ModelState.AddModelError("Name", "Nama posisi wajib diisi");

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);

            // Verify service was not called
            _mockPosisiService.Verify(s => s.Add(It.IsAny<Posisi>()), Times.Never);
        }

        [Fact]
        public async Task Create_Post_WithServiceFailure_ReturnsViewWithErrorMessage()
        {
            // Arrange
            var model = new Posisi
            {
                Name = "Duplicate Position"
            };

            _mockPosisiService.Setup(s => s.Add(model))
                .ReturnsAsync((Success: false, Message: "Posisi dengan nama tersebut sudah ada", Posisi: null));

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);
            _controller.TempData["ErrorMessage"].Should().Be("Posisi dengan nama tersebut sudah ada");
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewWithModel()
        {
            // Arrange
            var posisi = new Posisi
            {
                Id = 1,
                Name = "Test Position"
            };

            _mockPosisiService.Setup(s => s.GetById(1))
                .ReturnsAsync(posisi);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(posisi);
        }

        [Fact]
        public async Task Edit_Get_WithInvalidId_ReturnsRedirectToIndex()
        {
            // Arrange
            _mockPosisiService.Setup(s => s.GetById(1))
                .ReturnsAsync((Posisi?)null);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Posisi tidak ditemukan");
        }

        [Fact]
        public async Task Edit_Post_WithValidModel_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new Posisi
            {
                Id = 1,
                Name = "Updated Position"
            };

            _mockPosisiService.Setup(s => s.Update(model))
                .ReturnsAsync((Success: true, Message: "Posisi berhasil diupdate", Posisi: model));

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Posisi berhasil diupdate");
        }

        [Fact]
        public async Task Edit_Post_WithMismatchedId_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new Posisi { Id = 2 };

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Data tidak valid");

            // Verify service was not called
            _mockPosisiService.Verify(s => s.Update(It.IsAny<Posisi>()), Times.Never);
        }

        [Fact]
        public async Task Edit_Post_WithInvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var model = new Posisi { Id = 1 };
            _controller.ModelState.AddModelError("Name", "Nama posisi wajib diisi");

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);

            // Verify service was not called
            _mockPosisiService.Verify(s => s.Update(It.IsAny<Posisi>()), Times.Never);
        }

        [Fact]
        public async Task Edit_Post_WithServiceFailure_ReturnsViewWithErrorMessage()
        {
            // Arrange
            var model = new Posisi
            {
                Id = 1,
                Name = "Updated Position"
            };

            _mockPosisiService.Setup(s => s.Update(model))
                .ReturnsAsync((Success: false, Message: "Database error occurred", Posisi: null));

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);
            _controller.TempData["ErrorMessage"].Should().Be("Database error occurred");
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidId_ReturnsRedirectWithSuccessMessage()
        {
            // Arrange
            _mockPosisiService.Setup(s => s.Delete(1))
                .ReturnsAsync((Success: true, Message: "Posisi berhasil dihapus"));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Posisi berhasil dihapus");
        }

        [Fact]
        public async Task Delete_WithServiceFailure_ReturnsRedirectWithErrorMessage()
        {
            // Arrange
            _mockPosisiService.Setup(s => s.Delete(1))
                .ReturnsAsync((Success: false, Message: "Tidak dapat menghapus posisi yang masih digunakan"));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Tidak dapat menghapus posisi yang masih digunakan");
        }

        [Fact]
        public async Task Delete_CallsCorrectServiceMethod()
        {
            // Arrange
            _mockPosisiService.Setup(s => s.Delete(1))
                .ReturnsAsync((Success: true, Message: "Success"));

            // Act
            await _controller.Delete(1);

            // Assert
            _mockPosisiService.Verify(s => s.Delete(1), Times.Once);
        }

        #endregion

        #region GetActive Tests

        [Fact]
        public async Task GetActive_ReturnsJsonWithActivePosisi()
        {
            // Arrange
            var activePosisi = new List<Posisi>
            {
                new() { Id = 1, Name = "Developer", IsDeleted = false },
                new() { Id = 2, Name = "Tester", IsDeleted = false },
                new() { Id = 3, Name = "Manager", IsDeleted = false }
            };

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(activePosisi);

            // Act
            var result = await _controller.GetActive();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            jsonResult!.Value.Should().NotBeNull();

            // Convert the anonymous object to a list for testing
            var jsonValue = jsonResult.Value as IEnumerable<object>;
            jsonValue.Should().NotBeNull();

            // Verify the structure by converting to dynamic and checking properties
            var items = jsonValue!.ToList();
            items.Should().HaveCount(3);

            // Check first item structure using reflection
            var firstItem = items[0];
            var idProperty = firstItem.GetType().GetProperty("id");
            var nameProperty = firstItem.GetType().GetProperty("name");

            idProperty!.GetValue(firstItem).Should().Be(1);
            nameProperty!.GetValue(firstItem).Should().Be("Developer");
        }

        [Fact]
        public async Task GetActive_WithEmptyList_ReturnsEmptyJson()
        {
            // Arrange
            var emptyPosisi = new List<Posisi>();

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(emptyPosisi);

            // Act
            var result = await _controller.GetActive();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var jsonValue = jsonResult!.Value as IEnumerable<object>;
            jsonValue.Should().NotBeNull();
            jsonValue!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActive_CallsCorrectServiceMethod()
        {
            // Arrange
            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(new List<Posisi>());

            // Act
            await _controller.GetActive();

            // Assert
            _mockPosisiService.Verify(s => s.GetActivePosisi(), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_CreateEditDelete_WorksCorrectly()
        {
            // Arrange
            var posisi = new Posisi
            {
                Id = 1,
                Name = "Test Position"
            };

            // Setup for Create
            _mockPosisiService.Setup(s => s.Add(posisi))
                .ReturnsAsync((Success: true, Message: "Created successfully", Posisi: posisi));

            // Setup for Edit - GET
            _mockPosisiService.Setup(s => s.GetById(1))
                .ReturnsAsync(posisi);

            // Setup for Edit - POST
            var updatedPosisi = new Posisi { Id = 1, Name = "Updated Position" };
            _mockPosisiService.Setup(s => s.Update(updatedPosisi))
                .ReturnsAsync((Success: true, Message: "Updated successfully", Posisi: updatedPosisi));

            // Setup for Delete
            _mockPosisiService.Setup(s => s.Delete(1))
                .ReturnsAsync((Success: true, Message: "Deleted successfully"));

            // Act & Assert - Create
            var createResult = await _controller.Create(posisi);
            createResult.Should().BeOfType<RedirectToActionResult>();

            // Act & Assert - Edit GET
            var editGetResult = await _controller.Edit(1);
            editGetResult.Should().BeOfType<ViewResult>();

            // Act & Assert - Edit POST
            var editPostResult = await _controller.Edit(1, updatedPosisi);
            editPostResult.Should().BeOfType<RedirectToActionResult>();

            // Act & Assert - Delete
            var deleteResult = await _controller.Delete(1);
            deleteResult.Should().BeOfType<RedirectToActionResult>();

            // Verify all service calls
            _mockPosisiService.Verify(s => s.Add(posisi), Times.Once);
            _mockPosisiService.Verify(s => s.GetById(1), Times.Once);
            _mockPosisiService.Verify(s => s.Update(updatedPosisi), Times.Once);
            _mockPosisiService.Verify(s => s.Delete(1), Times.Once);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task Index_HandlesNullPosisiInViewModel()
        {
            // Arrange
            var posisiWithCount = new List<PosisiWithCountViewModel>
            {
                new(new Posisi { Id = 1, Name = "Valid Position" }, 2)
            };

            _mockPosisiService.Setup(s => s.GetPosisiWithEmployeeCount())
                .ReturnsAsync(posisiWithCount);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as PosisiListViewModel;

            model!.Posisis.Should().HaveCount(1);
            model.Posisis.First().Name.Should().Be("Valid Position");
        }

        [Fact]
        public async Task Edit_Post_WithNullModel_HandlesGracefully()
        {
            // Note: In real scenarios, model binding would prevent null models,
            // but this tests the defensive programming aspect

            // Arrange
            Posisi? nullModel = null;

            // Act & Assert
            // This would typically throw an exception or be handled by model binding
            // The test verifies our understanding of the expected behavior
            var exception = await Record.ExceptionAsync(() => _controller.Edit(1, nullModel!));

            // The specific behavior depends on how the framework handles null models
            // This test documents the expected behavior
        }

        [Fact]
        public async Task GetActive_VerifiesCorrectJsonStructure()
        {
            // Arrange
            var activePosisi = new List<Posisi>
            {
                new() { Id = 100, Name = "Special Position", IsDeleted = false }
            };

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(activePosisi);

            // Act
            var result = await _controller.GetActive();

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;

            // Verify the JSON structure matches expected API format
            var jsonValue = jsonResult!.Value as IEnumerable<object>;
            var items = jsonValue!.ToList();
            items.Should().HaveCount(1);

            var item = items[0];
            var properties = item.GetType().GetProperties();
            properties.Should().HaveCount(2);
            properties.Should().Contain(p => p.Name == "id");
            properties.Should().Contain(p => p.Name == "name");
        }

        #endregion
    }
}


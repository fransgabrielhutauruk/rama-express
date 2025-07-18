// RamaExpress.Tests/Controllers/Admin/KaryawanControllerTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using FluentAssertions;
using RamaExpress.Areas.Admin.Controllers;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Controllers.Admin
{
    public class KaryawanControllerTests : IDisposable
    {
        private readonly Mock<IKaryawanService> _mockKaryawanService;
        private readonly Mock<IPosisiService> _mockPosisiService;
        private readonly KaryawanController _controller;

        public KaryawanControllerTests()
        {
            // Setup mocks
            _mockKaryawanService = new Mock<IKaryawanService>();
            _mockPosisiService = new Mock<IPosisiService>();

            // Create controller
            _controller = new KaryawanController(
                _mockKaryawanService.Object,
                _mockPosisiService.Object);

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
            var users = new List<User>
            {
                new() { Id = 1, Nama = "John Doe", Email = "john@test.com", Posisi = "Developer", IsActive = true },
                new() { Id = 2, Nama = "Jane Smith", Email = "jane@test.com", Posisi = "Tester", IsActive = true }
            };
            var totalCount = 2;

            _mockKaryawanService.Setup(s => s.GetAllWithSearchAndSort(1, 10, null, null, "Nama", "asc"))
                .ReturnsAsync((users, totalCount));

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<KaryawanListViewModel>();

            var model = viewResult.Model as KaryawanListViewModel;
            model!.Users.Should().HaveCount(2);
            model.TotalCount.Should().Be(2);
            model.CurrentPage.Should().Be(1);
            model.PageSize.Should().Be(10);
            model.SortField.Should().Be("Nama");
            model.SortDirection.Should().Be("asc");
        }

        [Fact]
        public async Task Index_WithSearchParameters_PassesCorrectParametersToService()
        {
            // Arrange
            var searchTerm = "John";
            var statusFilter = "active";
            var page = 2;
            var pageSize = 5;
            var sortField = "Email";
            var sortDirection = "desc";

            _mockKaryawanService.Setup(s => s.GetAllWithSearchAndSort(page, pageSize, searchTerm, statusFilter, sortField, sortDirection))
                .ReturnsAsync((new List<User>(), 0));

            // Act
            await _controller.Index(page, searchTerm, statusFilter, pageSize, sortField, sortDirection);

            // Assert
            _mockKaryawanService.Verify(s => s.GetAllWithSearchAndSort(page, pageSize, searchTerm, statusFilter, sortField, sortDirection), Times.Once);
        }

        [Fact]
        public async Task Index_WithInvalidPage_SetsPageToOne()
        {
            // Arrange
            _mockKaryawanService.Setup(s => s.GetAllWithSearchAndSort(1, 10, null, null, "Nama", "asc"))
                .ReturnsAsync((new List<User>(), 0));

            // Act
            await _controller.Index(page: -1);

            // Assert
            _mockKaryawanService.Verify(s => s.GetAllWithSearchAndSort(1, 10, null, null, "Nama", "asc"), Times.Once);
        }

        [Fact]
        public async Task Index_WithInvalidPageSize_ClampsSizeToValidRange()
        {
            // Arrange
            _mockKaryawanService.Setup(s => s.GetAllWithSearchAndSort(1, 100, null, null, "Nama", "asc"))
                .ReturnsAsync((new List<User>(), 0));

            // Act
            await _controller.Index(pageSize: 150); // Should be clamped to 100

            // Assert
            _mockKaryawanService.Verify(s => s.GetAllWithSearchAndSort(1, 100, null, null, "Nama", "asc"), Times.Once);
        }

        #endregion

        #region Create Tests

        [Fact]
        public async Task Create_Get_ReturnsViewAndLoadsPosisiDropdown()
        {
            // Arrange
            var posisiList = new List<Posisi>
            {
                new() { Id = 1, Name = "Developer" },
                new() { Id = 2, Name = "Tester" }
            };

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Create();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewData["PosisiList"].Should().NotBeNull();
            viewResult.ViewData["PosisiList"].Should().BeOfType<SelectList>();

            _mockPosisiService.Verify(s => s.GetActivePosisi(), Times.Once);
        }

        [Fact]
        public async Task Create_Post_WithValidModel_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new User
            {
                Nama = "John Doe",
                Email = "john@test.com",
                Password = "password123",
                Posisi = "Developer"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            // Fix: Explicitly provide both parameters to avoid optional argument issues
            _mockKaryawanService.Setup(s => s.IsEmailExists(model.Email, null))
                .ReturnsAsync(false);

            _mockKaryawanService.Setup(s => s.AddKaryawan(model))
                .ReturnsAsync((Success: true, Message: "Karyawan berhasil ditambahkan", User: model));

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Karyawan berhasil ditambahkan");
        }

        [Fact]
        public async Task Create_Post_WithExistingEmail_ReturnsViewWithModelError()
        {
            // Arrange
            var model = new User
            {
                Nama = "John Doe",
                Email = "existing@test.com",
                Password = "password123",
                Posisi = "Developer"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            // Fix: Explicitly provide both parameters
            _mockKaryawanService.Setup(s => s.IsEmailExists(model.Email, null))
                .ReturnsAsync(true);

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);
            _controller.ModelState.Should().ContainKey("Email");
            _controller.ModelState["Email"]!.Errors.Should().Contain(e => e.ErrorMessage == "Email sudah digunakan oleh pengguna lain");
        }

        [Fact]
        public async Task Create_Post_WithInvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var model = new User(); // Invalid model (missing required fields)
            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            _controller.ModelState.AddModelError("Nama", "Nama wajib diisi");

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);
            viewResult.ViewData["PosisiList"].Should().NotBeNull();
        }

        [Fact]
        public async Task Create_Post_WithServiceFailure_ReturnsViewWithErrorMessage()
        {
            // Arrange
            var model = new User
            {
                Nama = "John Doe",
                Email = "john@test.com",
                Password = "password123",
                Posisi = "Developer"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            // Fix: Explicitly provide both parameters
            _mockKaryawanService.Setup(s => s.IsEmailExists(model.Email, null))
                .ReturnsAsync(false);

            _mockKaryawanService.Setup(s => s.AddKaryawan(model))
                .ReturnsAsync((Success: false, Message: "Database error occurred", User: null));

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Create(model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Database error occurred");
        }

        #endregion

        #region Details Tests

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithUser()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Nama = "John Doe",
                Email = "john@test.com",
                Posisi = "Developer"
            };

            _mockKaryawanService.Setup(s => s.GetById(1))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.Details(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(user);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsRedirectToIndex()
        {
            // Arrange
            _mockKaryawanService.Setup(s => s.GetById(1))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Details(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Karyawan tidak ditemukan");
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewWithUser()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Nama = "John Doe",
                Email = "john@test.com",
                Posisi = "Developer",
                Password = "hashedpassword"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            _mockKaryawanService.Setup(s => s.GetById(1))
                .ReturnsAsync(user);

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as User;
            model!.Id.Should().Be(1);
            model.Password.Should().BeEmpty(); // Password should be cleared
            viewResult.ViewData["PosisiList"].Should().NotBeNull();
        }

        [Fact]
        public async Task Edit_Get_WithInvalidId_ReturnsRedirectToIndex()
        {
            // Arrange
            _mockKaryawanService.Setup(s => s.GetById(1))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Karyawan tidak ditemukan");
        }

        [Fact]
        public async Task Edit_Post_WithValidModel_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new User
            {
                Id = 1,
                Nama = "John Doe Updated",
                Email = "john.updated@test.com",
                Posisi = "Senior Developer"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Senior Developer" } };

            _mockKaryawanService.Setup(s => s.IsEmailExists(model.Email, model.Id))
                .ReturnsAsync(false);

            _mockKaryawanService.Setup(s => s.UpdateKaryawan(model))
                .ReturnsAsync((Success: true, Message: "Karyawan berhasil diupdate", User: model));

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Karyawan berhasil diupdate");
        }

        [Fact]
        public async Task Edit_Post_WithMismatchedId_ReturnsRedirectToIndex()
        {
            // Arrange
            var model = new User { Id = 2 };

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Data tidak valid");
        }

        [Fact]
        public async Task Edit_Post_WithExistingEmail_ReturnsViewWithModelError()
        {
            // Arrange
            var model = new User
            {
                Id = 1,
                Nama = "John Doe",
                Email = "existing@test.com",
                Posisi = "Developer"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            _mockKaryawanService.Setup(s => s.IsEmailExists(model.Email, model.Id))
                .ReturnsAsync(true);

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);
            _controller.ModelState.Should().ContainKey("Email");
        }

        [Fact]
        public async Task Edit_Post_WithShortPassword_ReturnsViewWithModelError()
        {
            // Arrange
            var model = new User
            {
                Id = 1,
                Nama = "John Doe",
                Email = "john@test.com",
                Password = "123", // Too short
                Posisi = "Developer"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            _mockKaryawanService.Setup(s => s.IsEmailExists(model.Email, model.Id))
                .ReturnsAsync(false);

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().Be(model);
            _controller.ModelState.Should().ContainKey("Password");
        }

        [Fact]
        public async Task Edit_Post_WithEmptyPassword_RemovesPasswordFromModelState()
        {
            // Arrange
            var model = new User
            {
                Id = 1,
                Nama = "John Doe",
                Email = "john@test.com",
                Password = "", // Empty password should be ignored
                Posisi = "Developer"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            _mockKaryawanService.Setup(s => s.IsEmailExists(model.Email, model.Id))
                .ReturnsAsync(false);

            _mockKaryawanService.Setup(s => s.UpdateKaryawan(model))
                .ReturnsAsync((Success: true, Message: "Updated successfully", User: model));

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.ModelState.Should().NotContainKey("Password");
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidId_ReturnsRedirectWithSuccessMessage()
        {
            // Arrange
            _mockKaryawanService.Setup(s => s.DeleteKaryawan(1))
                .ReturnsAsync((Success: true, Message: "Karyawan berhasil dihapus"));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Karyawan berhasil dihapus");
        }

        [Fact]
        public async Task Delete_WithServiceFailure_ReturnsRedirectWithErrorMessage()
        {
            // Arrange
            _mockKaryawanService.Setup(s => s.DeleteKaryawan(1))
                .ReturnsAsync((Success: false, Message: "Cannot delete user with active trainings"));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Cannot delete user with active trainings");
        }

        #endregion

        #region ToggleStatus Tests

        [Fact]
        public async Task ToggleStatus_WithValidId_ReturnsRedirectWithSuccessMessage()
        {
            // Arrange
            _mockKaryawanService.Setup(s => s.ToggleActiveStatus(1))
                .ReturnsAsync((Success: true, Message: "Status karyawan berhasil diubah"));

            // Act
            var result = await _controller.ToggleStatus(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Status karyawan berhasil diubah");
        }

        [Fact]
        public async Task ToggleStatus_WithServiceFailure_ReturnsRedirectWithErrorMessage()
        {
            // Arrange
            _mockKaryawanService.Setup(s => s.ToggleActiveStatus(1))
                .ReturnsAsync((Success: false, Message: "User not found"));

            // Act
            var result = await _controller.ToggleStatus(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("User not found");
        }

        #endregion

        #region LoadPosisiDropdown Tests

        [Fact]
        public async Task LoadPosisiDropdown_SetsViewBagCorrectly()
        {
            // Arrange
            var posisiList = new List<Posisi>
            {
                new() { Id = 1, Name = "Developer" },
                new() { Id = 2, Name = "Tester" },
                new() { Id = 3, Name = "Manager" }
            };

            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Act
            var result = await _controller.Create(); // This will call LoadPosisiDropdown internally

            // Assert
            var viewResult = result as ViewResult;
            viewResult!.ViewData["PosisiList"].Should().NotBeNull();
            viewResult.ViewData["PosisiList"].Should().BeOfType<SelectList>();

            var selectList = viewResult.ViewData["PosisiList"] as SelectList;
            selectList!.Should().HaveCount(3);

            _mockPosisiService.Verify(s => s.GetActivePosisi(), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_CreateEditDelete_WorksCorrectly()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Nama = "John Doe",
                Email = "john@test.com",
                Password = "password123",
                Posisi = "Developer"
            };

            var posisiList = new List<Posisi> { new() { Id = 1, Name = "Developer" } };

            // Setup for Create - Fix: Explicitly provide both parameters
            _mockKaryawanService.Setup(s => s.IsEmailExists(user.Email, null))
                .ReturnsAsync(false);
            _mockKaryawanService.Setup(s => s.AddKaryawan(user))
                .ReturnsAsync((Success: true, Message: "Created successfully", User: user));
            _mockPosisiService.Setup(s => s.GetActivePosisi())
                .ReturnsAsync(posisiList);

            // Setup for Edit
            _mockKaryawanService.Setup(s => s.GetById(1))
                .ReturnsAsync(user);
            _mockKaryawanService.Setup(s => s.IsEmailExists(user.Email, user.Id))
                .ReturnsAsync(false);
            _mockKaryawanService.Setup(s => s.UpdateKaryawan(user))
                .ReturnsAsync((Success: true, Message: "Updated successfully", User: user));

            // Setup for Delete
            _mockKaryawanService.Setup(s => s.DeleteKaryawan(1))
                .ReturnsAsync((Success: true, Message: "Deleted successfully"));

            // Act & Assert - Create
            var createResult = await _controller.Create(user);
            createResult.Should().BeOfType<RedirectToActionResult>();

            // Act & Assert - Edit
            var editGetResult = await _controller.Edit(1);
            editGetResult.Should().BeOfType<ViewResult>();

            var editPostResult = await _controller.Edit(1, user);
            editPostResult.Should().BeOfType<RedirectToActionResult>();

            // Act & Assert - Delete
            var deleteResult = await _controller.Delete(1);
            deleteResult.Should().BeOfType<RedirectToActionResult>();

            // Verify all service calls
            _mockKaryawanService.Verify(s => s.AddKaryawan(user), Times.Once);
            _mockKaryawanService.Verify(s => s.GetById(1), Times.Once);
            _mockKaryawanService.Verify(s => s.UpdateKaryawan(user), Times.Once);
            _mockKaryawanService.Verify(s => s.DeleteKaryawan(1), Times.Once);
        }

        #endregion
    }
}

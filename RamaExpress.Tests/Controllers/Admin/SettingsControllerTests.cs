// RamaExpress.Tests/Controllers/Admin/SettingsControllerTests.cs - CORRECTED VERSION
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using FluentAssertions;
using Moq;
using RamaExpress.Areas.Admin.Controllers;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Controllers.Admin
{
    // ✅ ADD: Testable version like KaryawanSettingsController
    public class TestableAdminSettingsController : SettingsController
    {
        private int? _testUserId;

        public TestableAdminSettingsController(ISettingsService settingsService) : base(settingsService)
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

    public class SettingsControllerTests : IDisposable
    {
        private readonly Mock<ISettingsService> _mockSettingsService;
        private readonly TestableAdminSettingsController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly Mock<ISession> _mockSession;

        public SettingsControllerTests()
        {
            // Setup mocks
            _mockSettingsService = new Mock<ISettingsService>();

            // ✅ CHANGED: Use testable controller
            _controller = new TestableAdminSettingsController(_mockSettingsService.Object);

            // Setup HttpContext
            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                _httpContext,
                Mock.Of<ITempDataProvider>());

            // Setup session
            _mockSession = new Mock<ISession>();
            _httpContext.Session = _mockSession.Object;
        }

        public void Dispose()
        {
            _controller?.Dispose();
        }

        #region Index GET Tests

        [Fact]
        public async Task Index_GET_WithValidAdminSession_ReturnsViewWithProfile()
        {
            // Arrange
            SetupValidSession();
            var mockProfile = CreateMockAdminProfile();
            _mockSettingsService.Setup(s => s.GetAdminProfile(1))
                .ReturnsAsync(mockProfile);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<SettingsViewModel>();

            var model = viewResult.Model as SettingsViewModel;
            model!.Profile.Should().NotBeNull();
            model.Profile.Id.Should().Be(1);
            model.Profile.Nama.Should().Be("Admin User");
            model.Profile.Email.Should().Be("admin@test.com");
        }

        [Fact]
        public async Task Index_GET_WithNoUserIdInSession_RedirectsToLogin()
        {
            // Arrange
            SetupNullSession();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("User");
            redirectResult.RouteValues!["area"].Should().Be("");
            _controller.TempData["ErrorMessage"].Should().Be("Sesi telah berakhir. Silakan login kembali.");
        }

        [Fact]
        public async Task Index_GET_WithNullProfile_RedirectsToLogin()
        {
            // Arrange
            SetupValidSession();
            _mockSettingsService.Setup(s => s.GetAdminProfile(1))
                .ReturnsAsync((AdminProfileViewModel?)null);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("User");
            _controller.TempData["ErrorMessage"].Should().Be("Data pengguna tidak ditemukan.");
        }

        [Fact]
        public async Task Index_GET_WithServiceException_RedirectsToHome()
        {
            // Arrange
            SetupValidSession();
            _mockSettingsService.Setup(s => s.GetAdminProfile(1))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            redirectResult.ControllerName.Should().Be("Home");
            _controller.TempData["ErrorMessage"].Should().Be("Terjadi kesalahan saat memuat halaman pengaturan.");
        }

        #endregion

        #region Index POST Tests

        [Fact]
        public async Task Index_POST_WithUpdateProfileAction_CallsUpdateProfileInternal()
        {
            // Arrange
            SetupValidSession();
            var model = CreateMockSettingsViewModel();
            _mockSettingsService.Setup(s => s.UpdateAdminProfile(1, model.Profile))
                .ReturnsAsync((true, "Profil berhasil diperbarui"));

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Profil berhasil diperbarui");

            // Verify service was called
            _mockSettingsService.Verify(s => s.UpdateAdminProfile(1, model.Profile), Times.Once);
        }

        [Fact]
        public async Task Index_POST_WithChangePasswordAction_CallsChangePasswordInternal()
        {
            // Arrange
            SetupValidSession();
            var model = CreateMockSettingsViewModel();
            _mockSettingsService.Setup(s => s.ChangePassword(1, model.ChangePassword))
                .ReturnsAsync((true, "Password berhasil diubah"));

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("User");
            _controller.TempData["SuccessMessage"].Should().Be("Password berhasil diubah. Silakan login ulang dengan password baru.");

            // Verify service was called
            _mockSettingsService.Verify(s => s.ChangePassword(1, model.ChangePassword), Times.Once);
        }

        [Fact]
        public async Task Index_POST_WithInvalidAction_RedirectsToIndex()
        {
            // Arrange
            SetupValidSession();
            var model = CreateMockSettingsViewModel();

            // Act
            var result = await _controller.Index(model, "invalidAction");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
        }

        [Fact]
        public async Task Index_POST_WithNoUserIdInSession_RedirectsToLogin()
        {
            // Arrange
            SetupNullSession();
            var model = CreateMockSettingsViewModel();

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("User");
            _controller.TempData["ErrorMessage"].Should().Be("Sesi telah berakhir. Silakan login kembali.");
        }

        [Fact]
        public async Task Index_POST_WithException_RedirectsToIndex()
        {
            // Arrange
            SetupValidSession();
            var model = CreateMockSettingsViewModel();
            _mockSettingsService.Setup(s => s.UpdateAdminProfile(1, model.Profile))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Terjadi kesalahan saat memperbarui profil.");
        }

        #endregion

        // Rest of the tests with the same pattern...
        // (UpdateProfileInternal tests, ChangePasswordInternal tests, etc.)

        #region Helper Methods

        private void SetupValidSession()
        {
            // ✅ CHANGED: Use testable controller method
            _controller.SetTestUserId(1);
        }

        private void SetupNullSession()
        {
            // ✅ CHANGED: Use testable controller method
            _controller.SetTestUserId(null);
        }

        private AdminProfileViewModel CreateMockAdminProfile()
        {
            return new AdminProfileViewModel
            {
                Id = 1,
                Nama = "Admin User",
                Email = "admin@test.com",
                Role = "admin",
                CreatedAt = DateTime.Now.AddDays(-30),
                UpdatedAt = DateTime.Now,
                IsActive = true
            };
        }

        private ChangePasswordViewModel CreateMockChangePasswordViewModel()
        {
            return new ChangePasswordViewModel
            {
                CurrentPassword = "oldpassword123",
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };
        }

        private SettingsViewModel CreateMockSettingsViewModel()
        {
            return new SettingsViewModel
            {
                Profile = CreateMockAdminProfile(),
                ChangePassword = CreateMockChangePasswordViewModel()
            };
        }

        #endregion
    }
}


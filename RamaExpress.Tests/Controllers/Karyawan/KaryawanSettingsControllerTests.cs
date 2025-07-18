// RamaExpress.Tests/Controllers/Karyawan/KaryawanSettingsControllerTests.cs - CORRECTED VERSION
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using FluentAssertions;
using Moq;
using RamaExpress.Areas.Karyawan.Controllers;
using RamaExpress.Areas.Karyawan.Data.Service;
using RamaExpress.Areas.Karyawan.Models;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Controllers.Karyawan
{
    // ✅ FIXED: Create a testable version of the controller
    public class TestableSettingsController : SettingsController
    {
        private int? _testUserId;

        public TestableSettingsController(IKaryawanSettingsService settingsService) : base(settingsService)
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

    public class KaryawanSettingsControllerTests : IDisposable
    {
        private readonly Mock<IKaryawanSettingsService> _mockSettingsService;
        private readonly TestableSettingsController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly Mock<ISession> _mockSession;

        public KaryawanSettingsControllerTests()
        {
            // Setup mocks
            _mockSettingsService = new Mock<IKaryawanSettingsService>();

            // ✅ FIXED: Use testable controller instead of mock
            _controller = new TestableSettingsController(_mockSettingsService.Object);

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

            // Setup session for role checking
            _mockSession = new Mock<ISession>();
            _httpContext.Session = _mockSession.Object;

            // Default setup for karyawan role
            SetupKaryawanRole();
        }

        public void Dispose()
        {
            _controller?.Dispose();
        }

        #region Index GET Tests

        [Fact]
        public async Task Index_GET_WithValidKaryawanSession_ReturnsViewWithProfile()
        {
            // Arrange
            SetupValidSession();
            var mockProfile = CreateMockKaryawanProfile();
            _mockSettingsService.Setup(s => s.GetKaryawanProfile(1))
                .ReturnsAsync(mockProfile);

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<KaryawanSettingsViewModel>();

            var model = viewResult.Model as KaryawanSettingsViewModel;
            model!.Profile.Should().NotBeNull();
            model.Profile.Id.Should().Be(1);
            model.Profile.Nama.Should().Be("John Doe");
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
        public async Task Index_GET_WithNonKaryawanRole_RedirectsToLogin()
        {
            // Arrange
            SetupValidSession();
            SetupAdminRole();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("User");
            _controller.TempData["ErrorMessage"].Should().Be("Akses tidak diizinkan.");
        }

        [Fact]
        public async Task Index_GET_WithNullProfile_RedirectsToLogin()
        {
            // Arrange
            SetupValidSession();
            _mockSettingsService.Setup(s => s.GetKaryawanProfile(1))
                .ReturnsAsync((KaryawanProfileViewModel?)null);

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
            _mockSettingsService.Setup(s => s.GetKaryawanProfile(1))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            redirectResult.ControllerName.Should().Be("Home");
            redirectResult.RouteValues!["area"].Should().Be("Karyawan");
            _controller.TempData["ErrorMessage"].Should().Be("Terjadi kesalahan saat memuat halaman pengaturan.");
        }

        #endregion

        #region Index POST Tests

        [Fact]
        public async Task Index_POST_WithUpdateProfileAction_CallsUpdateProfileInternal()
        {
            // Arrange
            SetupValidSession();
            var model = CreateMockKaryawanSettingsViewModel();
            var successResult = new ServiceResult<KaryawanProfileViewModel>
            {
                Success = true,
                Message = "Profil berhasil diperbarui",
                Data = model.Profile
            };
            _mockSettingsService.Setup(s => s.UpdateKaryawanProfile(1, model.Profile))
                .ReturnsAsync(successResult);

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Profil berhasil diperbarui");
        }

        [Fact]
        public async Task Index_POST_WithChangePasswordAction_CallsChangePasswordInternal()
        {
            // Arrange
            SetupValidSession();
            var model = CreateMockKaryawanSettingsViewModel();
            var successResult = new ServiceResult<string>
            {
                Success = true,
                Message = "Password berhasil diubah",
                Data = "Success"
            };
            _mockSettingsService.Setup(s => s.ChangePassword(1, model.ChangePassword))
                .ReturnsAsync(successResult);

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["SuccessMessage"].Should().Be("Password berhasil diubah.");
        }

        [Fact]
        public async Task Index_POST_WithInvalidAction_ReturnsErrorMessage()
        {
            // Arrange
            SetupValidSession();
            var model = CreateMockKaryawanSettingsViewModel();

            // Act
            var result = await _controller.Index(model, "invalidAction");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            _controller.TempData["ErrorMessage"].Should().Be("Aksi tidak valid.");
        }

        [Fact]
        public async Task Index_POST_WithNoUserIdInSession_RedirectsToLogin()
        {
            // Arrange
            SetupNullSession();
            var model = CreateMockKaryawanSettingsViewModel();

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
        public async Task Index_POST_WithNonKaryawanRole_RedirectsToLogin()
        {
            // Arrange
            SetupValidSession();
            SetupAdminRole();
            var model = CreateMockKaryawanSettingsViewModel();

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("User");
            _controller.TempData["ErrorMessage"].Should().Be("Akses tidak diizinkan.");
        }

        [Fact]
        public async Task Index_POST_WithException_RedirectsToIndex()
        {
            // Arrange
            SetupValidSession();
            var model = CreateMockKaryawanSettingsViewModel();
            _mockSettingsService.Setup(s => s.UpdateKaryawanProfile(1, model.Profile))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            // ✅ FIXED: Expect the specific error message from UpdateProfileInternal
            _controller.TempData["ErrorMessage"].Should().Be("Terjadi kesalahan saat memperbarui profil.");
        }

        #endregion

        #region UpdateProfileInternal Tests

        [Fact]
        public async Task UpdateProfile_WithValidData_ReturnsSuccessAndUpdatesSession()
        {
            // Arrange
            SetupValidSession();
            var profileModel = CreateMockKaryawanProfile();
            var successResult = new ServiceResult<KaryawanProfileViewModel>
            {
                Success = true,
                Message = "Profil berhasil diperbarui",
                Data = profileModel
            };
            _mockSettingsService.Setup(s => s.UpdateKaryawanProfile(1, profileModel))
                .ReturnsAsync(successResult);

            var model = new KaryawanSettingsViewModel { Profile = profileModel };

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["SuccessMessage"].Should().Be("Profil berhasil diperbarui");

            // Verify service was called
            _mockSettingsService.Verify(s => s.UpdateKaryawanProfile(1, profileModel), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_WithEmptyName_ReturnsError()
        {
            // Arrange
            SetupValidSession();
            var profileModel = CreateMockKaryawanProfile();
            profileModel.Nama = "";
            var model = new KaryawanSettingsViewModel { Profile = profileModel };

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Nama dan email wajib diisi.");

            // Verify service was not called
            _mockSettingsService.Verify(s => s.UpdateKaryawanProfile(It.IsAny<int>(), It.IsAny<KaryawanProfileViewModel>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProfile_WithEmptyEmail_ReturnsError()
        {
            // Arrange
            SetupValidSession();
            var profileModel = CreateMockKaryawanProfile();
            profileModel.Email = "";
            var model = new KaryawanSettingsViewModel { Profile = profileModel };

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Nama dan email wajib diisi.");
        }

        [Fact]
        public async Task UpdateProfile_WithWhitespaceFields_ReturnsError()
        {
            // Arrange
            SetupValidSession();
            var profileModel = CreateMockKaryawanProfile();
            profileModel.Nama = "   ";
            profileModel.Email = "   ";
            var model = new KaryawanSettingsViewModel { Profile = profileModel };

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Nama dan email wajib diisi.");
        }

        [Fact]
        public async Task UpdateProfile_WithServiceFailure_ReturnsErrorMessage()
        {
            // Arrange
            SetupValidSession();
            var profileModel = CreateMockKaryawanProfile();
            var failureResult = new ServiceResult<KaryawanProfileViewModel>
            {
                Success = false,
                Message = "Email sudah digunakan",
                Data = null
            };
            _mockSettingsService.Setup(s => s.UpdateKaryawanProfile(1, profileModel))
                .ReturnsAsync(failureResult);

            var model = new KaryawanSettingsViewModel { Profile = profileModel };

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Email sudah digunakan");
        }

        [Fact]
        public async Task UpdateProfile_WithException_ReturnsGenericError()
        {
            // Arrange
            SetupValidSession();
            var profileModel = CreateMockKaryawanProfile();
            _mockSettingsService.Setup(s => s.UpdateKaryawanProfile(1, profileModel))
                .ThrowsAsync(new Exception("Service error"));

            var model = new KaryawanSettingsViewModel { Profile = profileModel };

            // Act
            var result = await _controller.Index(model, "updateProfile");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            // ✅ FIXED: Expect the specific error message from UpdateProfileInternal
            _controller.TempData["ErrorMessage"].Should().Be("Terjadi kesalahan saat memperbarui profil.");
        }

        #endregion

        #region ChangePasswordInternal Tests

        [Fact]
        public async Task ChangePassword_WithValidData_ReturnsSuccess()
        {
            // Arrange
            SetupValidSession();
            var changePasswordModel = CreateMockChangePasswordViewModel();
            var successResult = new ServiceResult<string>
            {
                Success = true,
                Message = "Password berhasil diubah",
                Data = "Success"
            };
            _mockSettingsService.Setup(s => s.ChangePassword(1, changePasswordModel))
                .ReturnsAsync(successResult);

            var model = new KaryawanSettingsViewModel { ChangePassword = changePasswordModel };

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["SuccessMessage"].Should().Be("Password berhasil diubah.");

            // Verify service was called
            _mockSettingsService.Verify(s => s.ChangePassword(1, changePasswordModel), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_WithEmptyCurrentPassword_ReturnsError()
        {
            // Arrange
            SetupValidSession();
            var changePasswordModel = CreateMockChangePasswordViewModel();
            changePasswordModel.CurrentPassword = "";
            var model = new KaryawanSettingsViewModel { ChangePassword = changePasswordModel };

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Semua field password wajib diisi.");

            // Verify service was not called
            _mockSettingsService.Verify(s => s.ChangePassword(It.IsAny<int>(), It.IsAny<ChangePasswordViewModel>()), Times.Never);
        }

        [Fact]
        public async Task ChangePassword_WithEmptyNewPassword_ReturnsError()
        {
            // Arrange
            SetupValidSession();
            var changePasswordModel = CreateMockChangePasswordViewModel();
            changePasswordModel.NewPassword = "";
            var model = new KaryawanSettingsViewModel { ChangePassword = changePasswordModel };

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Semua field password wajib diisi.");
        }

        [Fact]
        public async Task ChangePassword_WithEmptyConfirmPassword_ReturnsError()
        {
            // Arrange
            SetupValidSession();
            var changePasswordModel = CreateMockChangePasswordViewModel();
            changePasswordModel.ConfirmNewPassword = "";
            var model = new KaryawanSettingsViewModel { ChangePassword = changePasswordModel };

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Semua field password wajib diisi.");
        }

        [Fact]
        public async Task ChangePassword_WithMismatchedPasswords_ReturnsError()
        {
            // Arrange
            SetupValidSession();
            var changePasswordModel = CreateMockChangePasswordViewModel();
            changePasswordModel.ConfirmNewPassword = "differentpassword";
            var model = new KaryawanSettingsViewModel { ChangePassword = changePasswordModel };

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Konfirmasi password tidak sesuai.");
        }

        [Fact]
        public async Task ChangePassword_WithShortPassword_ReturnsError()
        {
            // Arrange
            SetupValidSession();
            var changePasswordModel = CreateMockChangePasswordViewModel();
            changePasswordModel.NewPassword = "123";
            changePasswordModel.ConfirmNewPassword = "123";
            var model = new KaryawanSettingsViewModel { ChangePassword = changePasswordModel };

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Password baru minimal 6 karakter.");
        }

        [Fact]
        public async Task ChangePassword_WithServiceFailure_ReturnsErrorMessage()
        {
            // Arrange
            SetupValidSession();
            var changePasswordModel = CreateMockChangePasswordViewModel();
            var failureResult = new ServiceResult<string>
            {
                Success = false,
                Message = "Password lama tidak sesuai",
                Data = null
            };
            _mockSettingsService.Setup(s => s.ChangePassword(1, changePasswordModel))
                .ReturnsAsync(failureResult);

            var model = new KaryawanSettingsViewModel { ChangePassword = changePasswordModel };

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Password lama tidak sesuai");
        }

        [Fact]
        public async Task ChangePassword_WithException_ReturnsGenericError()
        {
            // Arrange
            SetupValidSession();
            var changePasswordModel = CreateMockChangePasswordViewModel();
            _mockSettingsService.Setup(s => s.ChangePassword(1, changePasswordModel))
                .ThrowsAsync(new Exception("Service error"));

            var model = new KaryawanSettingsViewModel { ChangePassword = changePasswordModel };

            // Act
            var result = await _controller.Index(model, "changePassword");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            // ✅ FIXED: Expect the specific error message from ChangePasswordInternal
            _controller.TempData["ErrorMessage"].Should().Be("Terjadi kesalahan saat mengubah password.");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_GetProfileUpdateProfile_WorksCorrectly()
        {
            // Arrange
            SetupValidSession();
            var mockProfile = CreateMockKaryawanProfile();
            _mockSettingsService.Setup(s => s.GetKaryawanProfile(1))
                .ReturnsAsync(mockProfile);

            var successResult = new ServiceResult<KaryawanProfileViewModel>
            {
                Success = true,
                Message = "Profil berhasil diperbarui",
                Data = mockProfile
            };
            _mockSettingsService.Setup(s => s.UpdateKaryawanProfile(1, It.IsAny<KaryawanProfileViewModel>()))
                .ReturnsAsync(successResult);

            // Act 1: Get profile
            var getResult = await _controller.Index();
            getResult.Should().BeOfType<ViewResult>();

            // Act 2: Update profile
            var updatedProfile = CreateMockKaryawanProfile();
            updatedProfile.Nama = "Updated Name";
            var model = new KaryawanSettingsViewModel { Profile = updatedProfile };
            var updateResult = await _controller.Index(model, "updateProfile");

            // Assert
            updateResult.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["SuccessMessage"].Should().Be("Profil berhasil diperbarui");
            _mockSettingsService.Verify(s => s.GetKaryawanProfile(1), Times.Once);
            _mockSettingsService.Verify(s => s.UpdateKaryawanProfile(1, It.IsAny<KaryawanProfileViewModel>()), Times.Once);
        }

        #endregion

        #region Helper Methods

        private void SetupValidSession()
        {
            // ✅ FIXED: Use the testable controller's method
            _controller.SetTestUserId(1);
        }

        private void SetupNullSession()
        {
            // ✅ FIXED: Use the testable controller's method
            _controller.SetTestUserId(null);
        }

        private void SetupKaryawanRole()
        {
            var roleBytes = System.Text.Encoding.UTF8.GetBytes("karyawan");
            _mockSession.Setup(s => s.TryGetValue("UserRole", out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    value = roleBytes;
                    return true;
                });
        }

        private void SetupAdminRole()
        {
            var roleBytes = System.Text.Encoding.UTF8.GetBytes("admin");
            _mockSession.Setup(s => s.TryGetValue("UserRole", out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    value = roleBytes;
                    return true;
                });
        }

        private KaryawanProfileViewModel CreateMockKaryawanProfile()
        {
            return new KaryawanProfileViewModel
            {
                Id = 1,
                Nama = "John Doe",
                Email = "john@test.com",
                Posisi = "Developer",
                Role = "karyawan",
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

        private KaryawanSettingsViewModel CreateMockKaryawanSettingsViewModel()
        {
            return new KaryawanSettingsViewModel
            {
                Profile = CreateMockKaryawanProfile(),
                ChangePassword = CreateMockChangePasswordViewModel()
            };
        }

        #endregion
    }
}



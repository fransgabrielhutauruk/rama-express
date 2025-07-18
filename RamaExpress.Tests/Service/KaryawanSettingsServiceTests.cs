using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using FluentAssertions;
using Moq;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using RamaExpress.Areas.Karyawan.Data.Service;
using RamaExpress.Areas.Karyawan.Models;

public class KaryawanSettingsServiceTests : IDisposable
{
    private readonly RamaExpressAppContext _context;
    private readonly Mock<ILogger<KaryawanSettingsService>> _mockLogger;
    private readonly KaryawanSettingsService _service;

    public KaryawanSettingsServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new RamaExpressAppContext(options);

        // Setup mocks
        _mockLogger = new Mock<ILogger<KaryawanSettingsService>>();

        // Create service
        _service = new KaryawanSettingsService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetKaryawanProfile Tests

    [Fact]
    public async Task GetKaryawanProfile_WithValidKaryawanUser_ReturnsProfile()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _service.GetKaryawanProfile(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Nama.Should().Be("John Doe");
        result.Email.Should().Be("john@test.com");
        result.Posisi.Should().Be("Developer");
        result.Role.Should().Be("karyawan");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetKaryawanProfile_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _service.GetKaryawanProfile(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetKaryawanProfile_WithDeletedUser_ReturnsNull()
    {
        // Arrange
        await SeedTestDataWithDeletedUser();

        // Act
        var result = await _service.GetKaryawanProfile(3);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetKaryawanProfile_WithAdminUser_ReturnsNull()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _service.GetKaryawanProfile(4); // Admin user

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetKaryawanProfile_WithInactiveUser_ReturnsProfileWithInactiveStatus()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _service.GetKaryawanProfile(2); // Inactive user

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetKaryawanProfile_WithDatabaseError_ReturnsNull()
    {
        // Arrange
        _context.Dispose(); // Force database error

        // Act
        var result = await _service.GetKaryawanProfile(1);

        // Assert
        result.Should().BeNull();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting karyawan profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region UpdateKaryawanProfile Tests

    [Fact]
    public async Task UpdateKaryawanProfile_WithValidData_ReturnsSuccessAndUpdatesProfile()
    {
        // Arrange
        await SeedTestData();
        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "John Doe Updated",
            Email = "john.updated@test.com"
        };

        // Act
        var result = await _service.UpdateKaryawanProfile(1, updateModel);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Profil berhasil diperbarui.");
        result.Data.Should().NotBeNull();
        result.Data!.Nama.Should().Be("John Doe Updated");
        result.Data.Email.Should().Be("john.updated@test.com");

        // Verify in database
        var updatedUser = await _context.User.FindAsync(1);
        updatedUser!.Nama.Should().Be("John Doe Updated");
        updatedUser.Email.Should().Be("john.updated@test.com");
        updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateKaryawanProfile_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        await SeedTestData();
        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "Test User",
            Email = "test@test.com"
        };

        // Act
        var result = await _service.UpdateKaryawanProfile(999, updateModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Pengguna tidak ditemukan atau tidak memiliki akses.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateKaryawanProfile_WithDeletedUser_ReturnsFailure()
    {
        // Arrange
        await SeedTestDataWithDeletedUser();
        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "Test User",
            Email = "test@test.com"
        };

        // Act
        var result = await _service.UpdateKaryawanProfile(3, updateModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Pengguna tidak ditemukan atau tidak memiliki akses.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateKaryawanProfile_WithAdminUser_ReturnsFailure()
    {
        // Arrange
        await SeedTestData();
        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "Admin User",
            Email = "admin@test.com"
        };

        // Act
        var result = await _service.UpdateKaryawanProfile(4, updateModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Pengguna tidak ditemukan atau tidak memiliki akses.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateKaryawanProfile_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        await SeedTestData();
        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "John Doe",
            Email = "jane@test.com" // Email already used by user 2
        };

        // Act
        var result = await _service.UpdateKaryawanProfile(1, updateModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email sudah digunakan oleh pengguna lain.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task UpdateKaryawanProfile_WithSameEmail_ReturnsSuccess()
    {
        // Arrange
        await SeedTestData();
        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "John Doe Updated",
            Email = "john@test.com" // Same email as current user
        };

        // Act
        var result = await _service.UpdateKaryawanProfile(1, updateModel);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Profil berhasil diperbarui.");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateKaryawanProfile_TrimsAndLowercasesEmailAndName()
    {
        // Arrange
        await SeedTestData();
        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "  John Doe Updated  ",
            Email = "  JOHN.UPDATED@TEST.COM  "
        };

        // Act
        var result = await _service.UpdateKaryawanProfile(1, updateModel);

        // Assert
        result.Success.Should().BeTrue();

        var updatedUser = await _context.User.FindAsync(1);
        updatedUser!.Nama.Should().Be("John Doe Updated");
        updatedUser.Email.Should().Be("john.updated@test.com");
    }

    [Fact]
    public async Task UpdateKaryawanProfile_WithDatabaseError_ReturnsFailure()
    {
        // Arrange
        await SeedTestData();
        _context.Dispose(); // Force database error

        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "Test",
            Email = "test@test.com"
        };

        // Act
        var result = await _service.UpdateKaryawanProfile(1, updateModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Terjadi kesalahan saat memperbarui profil.");
        result.Data.Should().BeNull();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error updating karyawan profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_ReturnsSuccessAndChangesPassword()
    {
        // Arrange
        await SeedTestDataWithHashedPasswords();
        var changePasswordModel = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword123",
            NewPassword = "newpassword123",
            ConfirmNewPassword = "newpassword123"
        };

        // Act
        var result = await _service.ChangePassword(1, changePasswordModel);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Password berhasil diubah.");
        result.Data.Should().Be("Success");

        // Verify password was changed in database
        var updatedUser = await _context.User.FindAsync(1);
        updatedUser!.Password.Should().NotBe("oldpassword123"); // Should be hashed
        updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));

        // Verify new password can be verified
        var hasher = new PasswordHasher<User>();
        var verificationResult = hasher.VerifyHashedPassword(updatedUser, updatedUser.Password, "newpassword123");
        verificationResult.Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_ReturnsFailure()
    {
        // Arrange
        await SeedTestDataWithHashedPasswords();
        var changePasswordModel = new ChangePasswordViewModel
        {
            CurrentPassword = "wrongpassword",
            NewPassword = "newpassword123",
            ConfirmNewPassword = "newpassword123"
        };

        // Act
        var result = await _service.ChangePassword(1, changePasswordModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Password saat ini tidak sesuai.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ChangePassword_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        await SeedTestData();
        var changePasswordModel = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword123",
            NewPassword = "newpassword123",
            ConfirmNewPassword = "newpassword123"
        };

        // Act
        var result = await _service.ChangePassword(999, changePasswordModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Pengguna tidak ditemukan atau tidak memiliki akses.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ChangePassword_WithDeletedUser_ReturnsFailure()
    {
        // Arrange
        await SeedTestDataWithDeletedUser();
        var changePasswordModel = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword123",
            NewPassword = "newpassword123",
            ConfirmNewPassword = "newpassword123"
        };

        // Act
        var result = await _service.ChangePassword(3, changePasswordModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Pengguna tidak ditemukan atau tidak memiliki akses.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ChangePassword_WithAdminUser_ReturnsFailure()
    {
        // Arrange
        await SeedTestData();
        var changePasswordModel = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword123",
            NewPassword = "newpassword123",
            ConfirmNewPassword = "newpassword123"
        };

        // Act
        var result = await _service.ChangePassword(4, changePasswordModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Pengguna tidak ditemukan atau tidak memiliki akses.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ChangePassword_WithDatabaseError_ReturnsFailure()
    {
        // Arrange
        await SeedTestDataWithHashedPasswords();
        _context.Dispose(); // Force database error

        var changePasswordModel = new ChangePasswordViewModel
        {
            CurrentPassword = "oldpassword123",
            NewPassword = "newpassword123",
            ConfirmNewPassword = "newpassword123"
        };

        // Act
        var result = await _service.ChangePassword(1, changePasswordModel);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Terjadi kesalahan saat mengubah password.");
        result.Data.Should().BeNull();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error changing password")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task GetKaryawanProfile_WithEmptyStringValues_HandlesGracefully()
    {
        // Arrange
        await SeedTestDataWithEmptyStringValues();

        // Act  
        var result = await _service.GetKaryawanProfile(5);

        // Assert
        result.Should().NotBeNull();
        result!.Nama.Should().Be(string.Empty); // Service converts empty to empty
        result.Email.Should().Be(string.Empty); // Service converts empty to empty  
        result.Role.Should().Be("karyawan"); // Service provides default role
    }

    [Fact]
    public async Task UpdateKaryawanProfile_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        await SeedTestData();

        // Act 1: Get initial profile
        var initialProfile = await _service.GetKaryawanProfile(1);
        initialProfile.Should().NotBeNull();

        // Act 2: Update profile
        var updateModel = new KaryawanProfileViewModel
        {
            Nama = "Updated Name",
            Email = "updated@test.com"
        };
        var updateResult = await _service.UpdateKaryawanProfile(1, updateModel);
        updateResult.Success.Should().BeTrue();

        // Act 3: Get updated profile
        var updatedProfile = await _service.GetKaryawanProfile(1);
        updatedProfile.Should().NotBeNull();
        updatedProfile!.Nama.Should().Be("Updated Name");
        updatedProfile.Email.Should().Be("updated@test.com");
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestData()
    {
        var users = new List<User>
        {
            new()
            {
                Id = 1, Nama = "John Doe", Email = "john@test.com",
                Role = "karyawan", Posisi = "Developer", IsActive = true,
                IsDeleted = false, CreatedAt = DateTime.Now.AddDays(-30),
                Password = "password123"
            },
            new()
            {
                Id = 2, Nama = "Jane Smith", Email = "jane@test.com",
                Role = "karyawan", Posisi = "Tester", IsActive = false,
                IsDeleted = false, CreatedAt = DateTime.Now.AddDays(-20),
                Password = "password123"
            },
            new()
            {
                Id = 4, Nama = "Admin User", Email = "admin@test.com",
                Role = "admin", Posisi = "Administrator", IsActive = true,
                IsDeleted = false, CreatedAt = DateTime.Now.AddDays(-10),
                Password = "adminpass123"
            }
        };

        _context.User.AddRange(users);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithDeletedUser()
    {
        await SeedTestData();

        var deletedUser = new User
        {
            Id = 3,
            Nama = "Deleted User",
            Email = "deleted@test.com",
            Role = "karyawan",
            Posisi = "Developer",
            IsActive = true,
            IsDeleted = true, // Marked as deleted
            CreatedAt = DateTime.Now.AddDays(-15),
            Password = "password123"
        };

        _context.User.Add(deletedUser);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithHashedPasswords()
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = 1,
            Nama = "John Doe",
            Email = "john@test.com",
            Role = "karyawan",
            Posisi = "Developer",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now.AddDays(-30)
        };

        // Hash the password
        user.Password = hasher.HashPassword(user, "oldpassword123");

        _context.User.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithNullValues()
    {
        var user = new User
        {
            Id = 5,
            Nama = null!, // Null name
            Email = null!, // Null email
            Role = "karyawan",
            Posisi = "Developer",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now.AddDays(-30),
            Password = "password123"
        };

        _context.User.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithEmptyStringValues()
    {
        var user = new User
        {
            Id = 5,
            Nama = "", // Empty string - satisfies [Required]
            Email = "", // Empty string - satisfies [Required]
            Role = "karyawan",
            Posisi = "Developer",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now.AddDays(-30),
            Password = "password123"
        };

        _context.User.Add(user);
        await _context.SaveChangesAsync();
    }

    #endregion
}

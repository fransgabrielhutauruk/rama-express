// RamaExpress.Tests/Models/Admin/UserTests.cs
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Models.Admin
{
    public class UserTests
    {
        #region Property Tests

        [Fact]
        public void User_DefaultValues_ShouldBeSetCorrectly()
        {
            // Act
            var user = new User();

            // Assert
            user.Id.Should().Be(0);
            user.Nama.Should().Be(string.Empty);
            user.Posisi.Should().Be(string.Empty);
            user.Email.Should().Be(string.Empty);
            user.Password.Should().BeNull();
            user.Role.Should().Be("karyawan");
            user.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            user.IsActive.Should().BeTrue();
            user.IsDeleted.Should().BeFalse();
            user.UpdatedAt.Should().BeNull();
            user.DeletedAt.Should().BeNull();
        }

        [Fact]
        public void User_PropertyAssignments_ShouldWorkCorrectly()
        {
            // Arrange
            var createdDate = DateTime.Now.AddDays(-30);
            var updatedDate = DateTime.Now.AddDays(-1);
            var deletedDate = DateTime.Now;

            // Act
            var user = new User
            {
                Id = 1,
                Nama = "John Doe",
                Posisi = "Senior Developer",
                Email = "john.doe@test.com",
                Password = "securepassword123",
                Role = "admin",
                CreatedAt = createdDate,
                IsActive = false,
                IsDeleted = true,
                UpdatedAt = updatedDate,
                DeletedAt = deletedDate
            };

            // Assert
            user.Id.Should().Be(1);
            user.Nama.Should().Be("John Doe");
            user.Posisi.Should().Be("Senior Developer");
            user.Email.Should().Be("john.doe@test.com");
            user.Password.Should().Be("securepassword123");
            user.Role.Should().Be("admin");
            user.CreatedAt.Should().Be(createdDate);
            user.IsActive.Should().BeFalse();
            user.IsDeleted.Should().BeTrue();
            user.UpdatedAt.Should().Be(updatedDate);
            user.DeletedAt.Should().Be(deletedDate);
        }

        [Fact]
        public void User_NullableProperties_ShouldAcceptNull()
        {
            // Act
            var user = new User
            {
                Password = null,
                UpdatedAt = null,
                DeletedAt = null
            };

            // Assert
            user.Password.Should().BeNull();
            user.UpdatedAt.Should().BeNull();
            user.DeletedAt.Should().BeNull();
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void User_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = "john@test.com",
                Password = "password123",
                Role = "karyawan"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void User_Nama_RequiredValidation_ShouldFail(string? nama)
        {
            // Arrange
            var user = new User
            {
                Nama = nama!,
                Posisi = "Developer",
                Email = "john@test.com"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Nama") &&
                vr.ErrorMessage == "Nama wajib diisi");
        }

        [Fact]
        public void User_Nama_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var user = new User
            {
                Nama = new string('A', 101), // 101 characters, exceeds max of 100
                Posisi = "Developer",
                Email = "john@test.com"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Nama") &&
                vr.ErrorMessage == "Nama maksimal 100 karakter");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void User_Posisi_RequiredValidation_ShouldFail(string? posisi)
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = posisi!,
                Email = "john@test.com"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Posisi") &&
                vr.ErrorMessage == "Posisi wajib diisi");
        }

        [Fact]
        public void User_Posisi_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = new string('A', 51), // 51 characters, exceeds max of 50
                Email = "john@test.com"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Posisi") &&
                vr.ErrorMessage == "Posisi maksimal 50 karakter");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void User_Email_RequiredValidation_ShouldFail(string? email)
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = email!
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Email") &&
                vr.ErrorMessage == "Email wajib diisi");
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("test@")]
        [InlineData("@test.com")]
        [InlineData("test.test")]
        [InlineData("test@test@test.com")] // Multiple @ symbols
        public void User_Email_FormatValidation_ShouldFail(string invalidEmail)
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = invalidEmail
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Email") &&
                vr.ErrorMessage == "Format email tidak valid");
        }

        [Theory]
        [InlineData("john@test.com")]
        [InlineData("john.doe@example.com")]
        [InlineData("test123@company.co.id")]
        [InlineData("user+tag@domain.org")]
        [InlineData("test..test@test.com")] // This is actually valid per RFC standards
        public void User_Email_FormatValidation_ShouldPass(string validEmail)
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = validEmail
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().NotContain(vr =>
                vr.MemberNames.Contains("Email") &&
                vr.ErrorMessage == "Format email tidak valid");
        }

        [Fact]
        public void User_Email_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var longEmail = new string('a', 92) + "@test.com"; // 101 characters, exceeds max of 100
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = longEmail
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Email") &&
                vr.ErrorMessage == "Email maksimal 100 karakter");
        }

        [Theory]
        [InlineData("1")]
        [InlineData("12")]
        [InlineData("123")]
        [InlineData("1234")]
        [InlineData("12345")]
        public void User_Password_MinLengthValidation_ShouldFail(string shortPassword)
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = "john@test.com",
                Password = shortPassword
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Password") &&
                vr.ErrorMessage == "Password minimal 6 karakter");
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("password123")]
        [InlineData("securePassword!@#")]
        public void User_Password_MinLengthValidation_ShouldPass(string validPassword)
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = "john@test.com",
                Password = validPassword
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().NotContain(vr =>
                vr.MemberNames.Contains("Password") &&
                vr.ErrorMessage == "Password minimal 6 karakter");
        }

        [Fact]
        public void User_Password_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = "john@test.com",
                Password = new string('a', 256) // 256 characters, exceeds max of 255
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Password"));
        }

        [Fact]
        public void User_Password_NullValidation_ShouldPass()
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = "john@test.com",
                Password = null
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().NotContain(vr => vr.MemberNames.Contains("Password"));
        }

        [Fact]
        public void User_Role_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Posisi = "Developer",
                Email = "john@test.com",
                Role = new string('a', 21) // 21 characters, exceeds max of 20
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Role") &&
                vr.ErrorMessage == "Role maksimal 20 karakter");
        }

        #endregion

        #region Display Attribute Tests

        [Fact]
        public void User_DisplayAttributes_ShouldBeCorrect()
        {
            // Arrange
            var type = typeof(User);

            // Act & Assert
            var namaProperty = type.GetProperty("Nama");
            var namaDisplayAttr = namaProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            namaDisplayAttr?.Name.Should().Be("Nama Lengkap");

            var posisiProperty = type.GetProperty("Posisi");
            var posisiDisplayAttr = posisiProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            posisiDisplayAttr?.Name.Should().Be("Posisi/Jabatan");

            var createdAtProperty = type.GetProperty("CreatedAt");
            var createdAtDisplayAttr = createdAtProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            createdAtDisplayAttr?.Name.Should().Be("Tanggal Bergabung");

            var isActiveProperty = type.GetProperty("IsActive");
            var isActiveDisplayAttr = isActiveProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            isActiveDisplayAttr?.Name.Should().Be("Status Aktif");

            var isDeletedProperty = type.GetProperty("IsDeleted");
            var isDeletedDisplayAttr = isDeletedProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            isDeletedDisplayAttr?.Name.Should().Be("Status Dihapus");

            var updatedAtProperty = type.GetProperty("UpdatedAt");
            var updatedAtDisplayAttr = updatedAtProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            updatedAtDisplayAttr?.Name.Should().Be("Terakhir Diupdate");

            var deletedAtProperty = type.GetProperty("DeletedAt");
            var deletedAtDisplayAttr = deletedAtProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            deletedAtDisplayAttr?.Name.Should().Be("Tanggal Dihapus");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void User_CompleteModel_ShouldWorkCorrectly()
        {
            // Arrange
            var createdDate = DateTime.Now.AddMonths(-6);
            var updatedDate = DateTime.Now.AddDays(-5);

            // Act
            var user = new User
            {
                Id = 1,
                Nama = "John Doe",
                Posisi = "Senior Software Developer",
                Email = "john.doe@company.com",
                Password = "securePassword123!",
                Role = "admin",
                CreatedAt = createdDate,
                IsActive = true,
                IsDeleted = false,
                UpdatedAt = updatedDate,
                DeletedAt = null
            };

            // Assert
            user.Id.Should().Be(1);
            user.Nama.Should().Be("John Doe");
            user.Posisi.Should().Be("Senior Software Developer");
            user.Email.Should().Be("john.doe@company.com");
            user.Password.Should().Be("securePassword123!");
            user.Role.Should().Be("admin");
            user.CreatedAt.Should().Be(createdDate);
            user.IsActive.Should().BeTrue();
            user.IsDeleted.Should().BeFalse();
            user.UpdatedAt.Should().Be(updatedDate);
            user.DeletedAt.Should().BeNull();

            // Validate model
            var validationResults = ValidateModel(user);
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void User_MultipleValidationErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var user = new User
            {
                Nama = "", // Required error
                Posisi = new string('A', 51), // Length error
                Email = "invalid-email", // Format error + Required if empty
                Password = "123", // MinLength error
                Role = new string('B', 21) // Length error
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().HaveCountGreaterOrEqualTo(4);
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Nama"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Posisi"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Email"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Password"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Role"));
        }

        [Fact]
        public void User_EmployeeProfile_ShouldWorkCorrectly()
        {
            // Test typical employee user
            // Arrange & Act
            var employee = new User
            {
                Nama = "Jane Smith",
                Posisi = "Software Developer",
                Email = "jane.smith@company.com",
                Password = "employee123",
                Role = "karyawan" // Default role
            };

            // Assert
            employee.Role.Should().Be("karyawan");
            employee.IsActive.Should().BeTrue(); // Default
            employee.IsDeleted.Should().BeFalse(); // Default
            employee.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));

            var validationResults = ValidateModel(employee);
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void User_AdminProfile_ShouldWorkCorrectly()
        {
            // Test admin user
            // Arrange & Act
            var admin = new User
            {
                Nama = "Administrator",
                Posisi = "System Administrator",
                Email = "admin@company.com",
                Password = "adminSecure123!",
                Role = "admin"
            };

            // Assert
            admin.Role.Should().Be("admin");
            admin.IsActive.Should().BeTrue(); // Default
            admin.IsDeleted.Should().BeFalse(); // Default

            var validationResults = ValidateModel(admin);
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void User_DeletedUser_ShouldWorkCorrectly()
        {
            // Test soft-deleted user scenario
            // Arrange & Act
            var deletedUser = new User
            {
                Nama = "Former Employee",
                Posisi = "Ex-Developer",
                Email = "former@company.com",
                Role = "karyawan",
                IsActive = false,
                IsDeleted = true,
                DeletedAt = DateTime.Now.AddDays(-30)
            };

            // Assert
            deletedUser.IsActive.Should().BeFalse();
            deletedUser.IsDeleted.Should().BeTrue();
            deletedUser.DeletedAt.Should().NotBeNull();
            deletedUser.DeletedAt.Should().BeCloseTo(DateTime.Now.AddDays(-30), TimeSpan.FromHours(1));

            var validationResults = ValidateModel(deletedUser);
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void User_EdgeCaseValues_ShouldWorkCorrectly()
        {
            // Test boundary values
            // Arrange & Act
            var user = new User
            {
                Nama = new string('A', 100), // Max length
                Posisi = new string('B', 50), // Max length
                Email = new string('c', 89) + "@test.com", // Close to max length
                Password = "123456", // Min length
                Role = new string('R', 20) // Max length
            };

            // Assert
            user.Nama.Should().HaveLength(100);
            user.Posisi.Should().HaveLength(50);
            user.Email.Should().HaveLength(98); // 89 + "@test.com" = 98
            user.Password.Should().HaveLength(6);
            user.Role.Should().HaveLength(20);

            var validationResults = ValidateModel(user);
            validationResults.Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private static List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        #endregion
    }
}




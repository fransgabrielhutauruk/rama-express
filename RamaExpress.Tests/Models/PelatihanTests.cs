// RamaExpress.Tests/Models/Admin/PelatihanTests.cs
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Models.Admin
{
    public class PelatihanTests
    {
        #region Property Tests

        [Fact]
        public void Pelatihan_DefaultValues_ShouldBeSetCorrectly()
        {
            // Act
            var pelatihan = new Pelatihan();

            // Assert
            pelatihan.Id.Should().Be(0);
            pelatihan.SkorMinimal.Should().Be(70);
            pelatihan.IsActive.Should().BeTrue();
            pelatihan.IsDeleted.Should().BeFalse();
            pelatihan.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            pelatihan.UpdatedAt.Should().BeNull();
            pelatihan.DeletedAt.Should().BeNull();
        }

        [Fact]
        public void Pelatihan_PropertyAssignments_ShouldWorkCorrectly()
        {
            // Arrange
            var testDate = DateTime.Now.AddDays(-1);
            var updateDate = DateTime.Now;

            // Act
            var pelatihan = new Pelatihan
            {
                Id = 1,
                Kode = "TEST001",
                Judul = "Test Training",
                Deskripsi = "Test Description",
                DurasiMenit = 60,
                SkorMinimal = 80,
                IsActive = false,
                CreatedAt = testDate,
                UpdatedAt = updateDate,
                IsDeleted = true,
                DeletedAt = updateDate
            };

            // Assert
            pelatihan.Id.Should().Be(1);
            pelatihan.Kode.Should().Be("TEST001");
            pelatihan.Judul.Should().Be("Test Training");
            pelatihan.Deskripsi.Should().Be("Test Description");
            pelatihan.DurasiMenit.Should().Be(60);
            pelatihan.SkorMinimal.Should().Be(80);
            pelatihan.IsActive.Should().BeFalse();
            pelatihan.CreatedAt.Should().Be(testDate);
            pelatihan.UpdatedAt.Should().Be(updateDate);
            pelatihan.IsDeleted.Should().BeTrue();
            pelatihan.DeletedAt.Should().Be(updateDate);
        }

        [Fact]
        public void Pelatihan_NavigationProperties_ShouldBeInitializable()
        {
            // Arrange & Act
            var pelatihan = new Pelatihan
            {
                PelatihanPosisis = new List<PelatihanPosisi>(),
                PelatihanMateris = new List<PelatihanMateri>(),
                PelatihanSoals = new List<PelatihanSoal>(),
                PelatihanProgresses = new List<PelatihanProgress>(),
                PelatihanHasils = new List<PelatihanHasil>(),
                Sertifikats = new List<Sertifikat>(),
                PelatihanSertifikat = new PelatihanSertifikat()
            };

            // Assert
            pelatihan.PelatihanPosisis.Should().NotBeNull().And.BeEmpty();
            pelatihan.PelatihanMateris.Should().NotBeNull().And.BeEmpty();
            pelatihan.PelatihanSoals.Should().NotBeNull().And.BeEmpty();
            pelatihan.PelatihanProgresses.Should().NotBeNull().And.BeEmpty();
            pelatihan.PelatihanHasils.Should().NotBeNull().And.BeEmpty();
            pelatihan.Sertifikats.Should().NotBeNull().And.BeEmpty();
            pelatihan.PelatihanSertifikat.Should().NotBeNull();
        }

        [Fact]
        public void Pelatihan_NullableProperties_ShouldAcceptNull()
        {
            // Act
            var pelatihan = new Pelatihan
            {
                Deskripsi = null,
                UpdatedAt = null,
                DeletedAt = null,
                PelatihanPosisis = null,
                PelatihanMateris = null,
                PelatihanSoals = null,
                PelatihanProgresses = null,
                PelatihanHasils = null,
                Sertifikats = null,
                PelatihanSertifikat = null
            };

            // Assert
            pelatihan.Deskripsi.Should().BeNull();
            pelatihan.UpdatedAt.Should().BeNull();
            pelatihan.DeletedAt.Should().BeNull();
            pelatihan.PelatihanPosisis.Should().BeNull();
            pelatihan.PelatihanMateris.Should().BeNull();
            pelatihan.PelatihanSoals.Should().BeNull();
            pelatihan.PelatihanProgresses.Should().BeNull();
            pelatihan.PelatihanHasils.Should().BeNull();
            pelatihan.Sertifikats.Should().BeNull();
            pelatihan.PelatihanSertifikat.Should().BeNull();
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void Pelatihan_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                Deskripsi = "Test Description",
                DurasiMenit = 60,
                SkorMinimal = 80
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Pelatihan_Kode_RequiredValidation_ShouldFail(string? kode)
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = kode,
                Judul = "Test Training",
                DurasiMenit = 60
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Kode") &&
                vr.ErrorMessage == "Kode pelatihan wajib diisi");
        }

        [Fact]
        public void Pelatihan_Kode_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = new string('A', 21), // 21 characters, exceeds max of 20
                Judul = "Test Training",
                DurasiMenit = 60
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Kode") &&
                vr.ErrorMessage == "Kode maksimal 20 karakter");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Pelatihan_Judul_RequiredValidation_ShouldFail(string? judul)
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "TEST001",
                Judul = judul,
                DurasiMenit = 60
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Judul") &&
                vr.ErrorMessage == "Judul pelatihan wajib diisi");
        }

        [Fact]
        public void Pelatihan_Judul_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "TEST001",
                Judul = new string('A', 201), // 201 characters, exceeds max of 200
                DurasiMenit = 60
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Judul") &&
                vr.ErrorMessage == "Judul maksimal 200 karakter");
        }

        [Fact]
        public void Pelatihan_Deskripsi_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                Deskripsi = new string('A', 501), // 501 characters, exceeds max of 500
                DurasiMenit = 60
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("Deskripsi") &&
                vr.ErrorMessage == "Deskripsi maksimal 500 karakter");
        }

        [Fact]
        public void Pelatihan_Deskripsi_NullOrEmptyValidation_ShouldPass()
        {
            // Arrange
            var pelatihan1 = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                Deskripsi = null,
                DurasiMenit = 60
            };

            var pelatihan2 = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                Deskripsi = "",
                DurasiMenit = 60
            };

            // Act
            var validationResults1 = ValidateModel(pelatihan1);
            var validationResults2 = ValidateModel(pelatihan2);

            // Assert
            validationResults1.Should().NotContain(vr => vr.MemberNames.Contains("Deskripsi"));
            validationResults2.Should().NotContain(vr => vr.MemberNames.Contains("Deskripsi"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(10000)]
        public void Pelatihan_DurasiMenit_RangeValidation_ShouldFail(int durasi)
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                DurasiMenit = durasi
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("DurasiMenit") &&
                vr.ErrorMessage == "Durasi harus antara 1-9999 menit");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(60)]
        [InlineData(9999)]
        public void Pelatihan_DurasiMenit_RangeValidation_ShouldPass(int durasi)
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                DurasiMenit = durasi
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().NotContain(vr => vr.MemberNames.Contains("DurasiMenit"));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Pelatihan_SkorMinimal_RangeValidation_ShouldFail(int skor)
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                DurasiMenit = 60,
                SkorMinimal = skor
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("SkorMinimal") &&
                vr.ErrorMessage == "Skor minimal harus antara 0-100");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public void Pelatihan_SkorMinimal_RangeValidation_ShouldPass(int skor)
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "TEST001",
                Judul = "Test Training",
                DurasiMenit = 60,
                SkorMinimal = skor
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().NotContain(vr => vr.MemberNames.Contains("SkorMinimal"));
        }

        #endregion

        #region Display Attribute Tests

        [Fact]
        public void Pelatihan_DisplayAttributes_ShouldBeCorrect()
        {
            // Arrange
            var type = typeof(Pelatihan);

            // Act & Assert
            var kodeProperty = type.GetProperty("Kode");
            var kodeDisplayAttr = kodeProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            kodeDisplayAttr?.Name.Should().Be("Kode Pelatihan");

            var judulProperty = type.GetProperty("Judul");
            var judulDisplayAttr = judulProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            judulDisplayAttr?.Name.Should().Be("Judul Pelatihan");

            var deskripsiProperty = type.GetProperty("Deskripsi");
            var deskripsiDisplayAttr = deskripsiProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            deskripsiDisplayAttr?.Name.Should().Be("Deskripsi");

            var durasiProperty = type.GetProperty("DurasiMenit");
            var durasiDisplayAttr = durasiProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            durasiDisplayAttr?.Name.Should().Be("Durasi (Menit)");

            var skorProperty = type.GetProperty("SkorMinimal");
            var skorDisplayAttr = skorProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            skorDisplayAttr?.Name.Should().Be("Skor Minimal Lulus (%)");

            var activeProperty = type.GetProperty("IsActive");
            var activeDisplayAttr = activeProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            activeDisplayAttr?.Name.Should().Be("Status Aktif");

            var createdProperty = type.GetProperty("CreatedAt");
            var createdDisplayAttr = createdProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            createdDisplayAttr?.Name.Should().Be("Tanggal Dibuat");

            var updatedProperty = type.GetProperty("UpdatedAt");
            var updatedDisplayAttr = updatedProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            updatedDisplayAttr?.Name.Should().Be("Tanggal Diupdate");

            var deletedProperty = type.GetProperty("IsDeleted");
            var deletedDisplayAttr = deletedProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            deletedDisplayAttr?.Name.Should().Be("Status Dihapus");

            var deletedAtProperty = type.GetProperty("DeletedAt");
            var deletedAtDisplayAttr = deletedAtProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            deletedAtDisplayAttr?.Name.Should().Be("Tanggal Dihapus");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Pelatihan_CompleteModel_ShouldWorkCorrectly()
        {
            // Arrange
            var createdDate = DateTime.Now.AddDays(-1);
            var updatedDate = DateTime.Now;

            // Act
            var pelatihan = new Pelatihan
            {
                Id = 1,
                Kode = "TRAIN001",
                Judul = "Complete Training Course",
                Deskripsi = "This is a comprehensive training course description.",
                DurasiMenit = 120,
                SkorMinimal = 75,
                IsActive = true,
                CreatedAt = createdDate,
                UpdatedAt = updatedDate,
                IsDeleted = false,
                DeletedAt = null
            };

            // Initialize navigation properties
            pelatihan.PelatihanPosisis = new List<PelatihanPosisi>();
            pelatihan.PelatihanMateris = new List<PelatihanMateri>();
            pelatihan.PelatihanSoals = new List<PelatihanSoal>();
            pelatihan.PelatihanProgresses = new List<PelatihanProgress>();
            pelatihan.PelatihanHasils = new List<PelatihanHasil>();
            pelatihan.Sertifikats = new List<Sertifikat>();

            // Assert
            pelatihan.Id.Should().Be(1);
            pelatihan.Kode.Should().Be("TRAIN001");
            pelatihan.Judul.Should().Be("Complete Training Course");
            pelatihan.Deskripsi.Should().Be("This is a comprehensive training course description.");
            pelatihan.DurasiMenit.Should().Be(120);
            pelatihan.SkorMinimal.Should().Be(75);
            pelatihan.IsActive.Should().BeTrue();
            pelatihan.CreatedAt.Should().Be(createdDate);
            pelatihan.UpdatedAt.Should().Be(updatedDate);
            pelatihan.IsDeleted.Should().BeFalse();
            pelatihan.DeletedAt.Should().BeNull();

            // Validate model
            var validationResults = ValidateModel(pelatihan);
            validationResults.Should().BeEmpty();

            // Verify navigation properties
            pelatihan.PelatihanPosisis.Should().NotBeNull();
            pelatihan.PelatihanMateris.Should().NotBeNull();
            pelatihan.PelatihanSoals.Should().NotBeNull();
            pelatihan.PelatihanProgresses.Should().NotBeNull();
            pelatihan.PelatihanHasils.Should().NotBeNull();
            pelatihan.Sertifikats.Should().NotBeNull();
        }

        [Fact]
        public void Pelatihan_MultipleValidationErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = null, // Required error
                Judul = "", // Required error
                Deskripsi = new string('A', 501), // Length error
                DurasiMenit = 0, // Range error
                SkorMinimal = 101 // Range error
            };

            // Act
            var validationResults = ValidateModel(pelatihan);

            // Assert
            validationResults.Should().HaveCount(5);
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Kode"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Judul"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Deskripsi"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("DurasiMenit"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("SkorMinimal"));
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


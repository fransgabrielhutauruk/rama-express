// RamaExpress.Tests/Models/Admin/PelatihanSertifikatTests.cs
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Models.Admin
{
    public class PelatihanSertifikatTests
    {
        #region Property Tests

        [Fact]
        public void PelatihanSertifikat_DefaultValues_ShouldBeSetCorrectly()
        {
            // Act
            var sertifikat = new PelatihanSertifikat();

            // Assert
            sertifikat.Id.Should().Be(0);
            sertifikat.PelatihanId.Should().Be(0);
            sertifikat.IsSertifikatActive.Should().BeTrue();
            sertifikat.CertificateNumberFormat.Should().Be("CERT-{YEAR}-{MONTH}-{INCREMENT}");
            sertifikat.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            sertifikat.UpdatedAt.Should().BeNull();
            sertifikat.Pelatihan.Should().BeNull();
        }

        [Fact]
        public void PelatihanSertifikat_PropertyAssignments_ShouldWorkCorrectly()
        {
            // Arrange
            var createdDate = DateTime.Now.AddDays(-1);
            var updatedDate = DateTime.Now;
            var pelatihan = new Pelatihan { Id = 1, Judul = "Test Training" };

            // Act
            var sertifikat = new PelatihanSertifikat
            {
                Id = 1,
                PelatihanId = 1,
                IsSertifikatActive = false,
                TemplateName = "Test Certificate Template",
                TemplateDescription = "Test description for certificate",
                ExpirationType = "months",
                ExpirationDuration = 12,
                ExpirationUnit = "months",
                CertificateNumberFormat = "CUSTOM-{YEAR}-{INCREMENT}",
                CreatedAt = createdDate,
                UpdatedAt = updatedDate,
                Pelatihan = pelatihan
            };

            // Assert
            sertifikat.Id.Should().Be(1);
            sertifikat.PelatihanId.Should().Be(1);
            sertifikat.IsSertifikatActive.Should().BeFalse();
            sertifikat.TemplateName.Should().Be("Test Certificate Template");
            sertifikat.TemplateDescription.Should().Be("Test description for certificate");
            sertifikat.ExpirationType.Should().Be("months");
            sertifikat.ExpirationDuration.Should().Be(12);
            sertifikat.ExpirationUnit.Should().Be("months");
            sertifikat.CertificateNumberFormat.Should().Be("CUSTOM-{YEAR}-{INCREMENT}");
            sertifikat.CreatedAt.Should().Be(createdDate);
            sertifikat.UpdatedAt.Should().Be(updatedDate);
            sertifikat.Pelatihan.Should().Be(pelatihan);
        }

        [Fact]
        public void PelatihanSertifikat_NullableProperties_ShouldAcceptNull()
        {
            // Act
            var sertifikat = new PelatihanSertifikat
            {
                TemplateDescription = null,
                ExpirationDuration = null,
                ExpirationUnit = null,
                UpdatedAt = null,
                Pelatihan = null
            };

            // Assert
            sertifikat.TemplateDescription.Should().BeNull();
            sertifikat.ExpirationDuration.Should().BeNull();
            sertifikat.ExpirationUnit.Should().BeNull();
            sertifikat.UpdatedAt.Should().BeNull();
            sertifikat.Pelatihan.Should().BeNull();
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void PelatihanSertifikat_ValidModel_ShouldPassValidation()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                ExpirationType = "never"
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void PelatihanSertifikat_PelatihanId_CanBeZero()
        {
            // Note: int properties with [Required] attribute don't validate as required when set to 0
            // because 0 is a valid integer value. This test verifies the current behavior.

            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 0, // This is technically valid for int types
                TemplateName = "Certificate Template",
                ExpirationType = "never"
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            // The validation should not fail for PelatihanId = 0 because it's a valid int value
            validationResults.Should().NotContain(vr => vr.MemberNames.Contains("PelatihanId"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void PelatihanSertifikat_TemplateName_RequiredValidation_ShouldFail(string? templateName)
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = templateName!,
                ExpirationType = "never"
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("TemplateName") &&
                vr.ErrorMessage == "Nama template sertifikat wajib diisi");
        }

        [Fact]
        public void PelatihanSertifikat_TemplateName_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = new string('A', 201), // 201 characters, exceeds max of 200
                ExpirationType = "never"
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("TemplateName") &&
                vr.ErrorMessage == "Nama template maksimal 200 karakter");
        }

        [Fact]
        public void PelatihanSertifikat_TemplateDescription_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                TemplateDescription = new string('A', 501), // 501 characters, exceeds max of 500
                ExpirationType = "never"
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("TemplateDescription") &&
                vr.ErrorMessage == "Deskripsi template maksimal 500 karakter");
        }

        [Fact]
        public void PelatihanSertifikat_TemplateDescription_NullOrEmptyValidation_ShouldPass()
        {
            // Arrange
            var sertifikat1 = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                TemplateDescription = null,
                ExpirationType = "never"
            };

            var sertifikat2 = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                TemplateDescription = "",
                ExpirationType = "never"
            };

            // Act
            var validationResults1 = ValidateModel(sertifikat1);
            var validationResults2 = ValidateModel(sertifikat2);

            // Assert
            validationResults1.Should().NotContain(vr => vr.MemberNames.Contains("TemplateDescription"));
            validationResults2.Should().NotContain(vr => vr.MemberNames.Contains("TemplateDescription"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void PelatihanSertifikat_ExpirationType_RequiredValidation_ShouldFail(string? expirationType)
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                ExpirationType = expirationType!
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("ExpirationType") &&
                vr.ErrorMessage == "Tipe kadaluarsa wajib dipilih");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1000)]
        public void PelatihanSertifikat_ExpirationDuration_RangeValidation_ShouldFail(int duration)
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                ExpirationType = "months",
                ExpirationDuration = duration
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("ExpirationDuration") &&
                vr.ErrorMessage == "Durasi harus antara 1-999");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(12)]
        [InlineData(999)]
        public void PelatihanSertifikat_ExpirationDuration_RangeValidation_ShouldPass(int duration)
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                ExpirationType = "months",
                ExpirationDuration = duration
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().NotContain(vr => vr.MemberNames.Contains("ExpirationDuration"));
        }

        [Fact]
        public void PelatihanSertifikat_ExpirationUnit_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                ExpirationType = "months",
                ExpirationUnit = new string('A', 51) // 51 characters, exceeds max of 50
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("ExpirationUnit") &&
                vr.ErrorMessage == "Unit kadaluarsa maksimal 50 karakter");
        }

        [Fact]
        public void PelatihanSertifikat_CertificateNumberFormat_MaxLengthValidation_ShouldFail()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                PelatihanId = 1,
                TemplateName = "Certificate Template",
                ExpirationType = "never",
                CertificateNumberFormat = new string('A', 101) // 101 characters, exceeds max of 100
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().Contain(vr =>
                vr.MemberNames.Contains("CertificateNumberFormat") &&
                vr.ErrorMessage == "Format nomor maksimal 100 karakter");
        }

        #endregion

        #region Helper Method Tests

        [Fact]
        public void CalculateExpirationDate_WithNeverExpirationType_ShouldReturnNull()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "never"
            };
            var issueDate = DateTime.Now;

            // Act
            var result = sertifikat.CalculateExpirationDate(issueDate);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void CalculateExpirationDate_WithMonthsType_ShouldAddMonths()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "months",
                ExpirationDuration = 12
            };
            var issueDate = new DateTime(2024, 1, 15);

            // Act
            var result = sertifikat.CalculateExpirationDate(issueDate);

            // Assert
            result.Should().Be(new DateTime(2025, 1, 15));
        }

        [Fact]
        public void CalculateExpirationDate_WithYearsType_ShouldAddYears()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "years",
                ExpirationDuration = 2
            };
            var issueDate = new DateTime(2024, 1, 15);

            // Act
            var result = sertifikat.CalculateExpirationDate(issueDate);

            // Assert
            result.Should().Be(new DateTime(2026, 1, 15));
        }

        [Fact]
        public void CalculateExpirationDate_WithMonthsTypeButNullDuration_ShouldReturnNull()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "months",
                ExpirationDuration = null
            };
            var issueDate = DateTime.Now;

            // Act
            var result = sertifikat.CalculateExpirationDate(issueDate);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void CalculateExpirationDate_WithYearsTypeButNullDuration_ShouldReturnNull()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "years",
                ExpirationDuration = null
            };
            var issueDate = DateTime.Now;

            // Act
            var result = sertifikat.CalculateExpirationDate(issueDate);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void CalculateExpirationDate_WithUnknownType_ShouldReturnNull()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "unknown",
                ExpirationDuration = 5
            };
            var issueDate = DateTime.Now;

            // Act
            var result = sertifikat.CalculateExpirationDate(issueDate);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetExpirationDisplayText_WithNeverType_ShouldReturnCorrectText()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "never"
            };

            // Act
            var result = sertifikat.GetExpirationDisplayText();

            // Assert
            result.Should().Be("Tidak Ada Kadaluarsa");
        }

        [Fact]
        public void GetExpirationDisplayText_WithMonthsType_ShouldReturnCorrectText()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "months",
                ExpirationDuration = 12
            };

            // Act
            var result = sertifikat.GetExpirationDisplayText();

            // Assert
            result.Should().Be("12 bulan");
        }

        [Fact]
        public void GetExpirationDisplayText_WithYearsType_ShouldReturnCorrectText()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "years",
                ExpirationDuration = 2
            };

            // Act
            var result = sertifikat.GetExpirationDisplayText();

            // Assert
            result.Should().Be("2 tahun");
        }

        [Fact]
        public void GetExpirationDisplayText_WithNullDuration_ShouldReturnNotDetermined()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                ExpirationType = "months",
                ExpirationDuration = null
            };

            // Act
            var result = sertifikat.GetExpirationDisplayText();

            // Assert
            result.Should().Be("Tidak Ditentukan");
        }

        #endregion

        #region Display Attribute Tests

        [Fact]
        public void PelatihanSertifikat_DisplayAttributes_ShouldBeCorrect()
        {
            // Arrange
            var type = typeof(PelatihanSertifikat);

            // Act & Assert
            var activeProperty = type.GetProperty("IsSertifikatActive");
            var activeDisplayAttr = activeProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            activeDisplayAttr?.Name.Should().Be("Status Sertifikat");

            var templateNameProperty = type.GetProperty("TemplateName");
            var templateNameDisplayAttr = templateNameProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            templateNameDisplayAttr?.Name.Should().Be("Nama Template Sertifikat");

            var templateDescProperty = type.GetProperty("TemplateDescription");
            var templateDescDisplayAttr = templateDescProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            templateDescDisplayAttr?.Name.Should().Be("Deskripsi Template");

            var expirationTypeProperty = type.GetProperty("ExpirationType");
            var expirationTypeDisplayAttr = expirationTypeProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            expirationTypeDisplayAttr?.Name.Should().Be("Tipe Kadaluarsa");

            var durationProperty = type.GetProperty("ExpirationDuration");
            var durationDisplayAttr = durationProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            durationDisplayAttr?.Name.Should().Be("Durasi Kadaluarsa");

            var unitProperty = type.GetProperty("ExpirationUnit");
            var unitDisplayAttr = unitProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            unitDisplayAttr?.Name.Should().Be("Unit Kadaluarsa");

            var formatProperty = type.GetProperty("CertificateNumberFormat");
            var formatDisplayAttr = formatProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            formatDisplayAttr?.Name.Should().Be("Format Nomor Sertifikat");

            var createdProperty = type.GetProperty("CreatedAt");
            var createdDisplayAttr = createdProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            createdDisplayAttr?.Name.Should().Be("Tanggal Dibuat");

            var updatedProperty = type.GetProperty("UpdatedAt");
            var updatedDisplayAttr = updatedProperty?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            updatedDisplayAttr?.Name.Should().Be("Tanggal Diupdate");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void PelatihanSertifikat_CompleteModel_ShouldWorkCorrectly()
        {
            // Arrange
            var createdDate = DateTime.Now.AddDays(-1);
            var updatedDate = DateTime.Now;
            var issueDate = DateTime.Now;

            // Act
            var sertifikat = new PelatihanSertifikat
            {
                Id = 1,
                PelatihanId = 1,
                IsSertifikatActive = true,
                TemplateName = "Professional Certificate Template",
                TemplateDescription = "This is a comprehensive certificate template for professional training.",
                ExpirationType = "years",
                ExpirationDuration = 3,
                ExpirationUnit = "years",
                CertificateNumberFormat = "PROF-{YEAR}-{MONTH}-{INCREMENT}",
                CreatedAt = createdDate,
                UpdatedAt = updatedDate
            };

            // Assert
            sertifikat.Id.Should().Be(1);
            sertifikat.PelatihanId.Should().Be(1);
            sertifikat.IsSertifikatActive.Should().BeTrue();
            sertifikat.TemplateName.Should().Be("Professional Certificate Template");
            sertifikat.TemplateDescription.Should().Be("This is a comprehensive certificate template for professional training.");
            sertifikat.ExpirationType.Should().Be("years");
            sertifikat.ExpirationDuration.Should().Be(3);
            sertifikat.ExpirationUnit.Should().Be("years");
            sertifikat.CertificateNumberFormat.Should().Be("PROF-{YEAR}-{MONTH}-{INCREMENT}");
            sertifikat.CreatedAt.Should().Be(createdDate);
            sertifikat.UpdatedAt.Should().Be(updatedDate);

            // Validate model
            var validationResults = ValidateModel(sertifikat);
            validationResults.Should().BeEmpty();

            // Test helper methods
            var expirationDate = sertifikat.CalculateExpirationDate(issueDate);
            expirationDate.Should().Be(issueDate.AddYears(3));

            var displayText = sertifikat.GetExpirationDisplayText();
            displayText.Should().Be("3 tahun");
        }

        [Fact]
        public void PelatihanSertifikat_MultipleValidationErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var sertifikat = new PelatihanSertifikat
            {
                // Note: PelatihanId = 0 is valid for int types, so no validation error expected
                PelatihanId = 1, // Set to valid value to avoid confusion
                TemplateName = "", // Required error
                TemplateDescription = new string('A', 501), // Length error
                ExpirationType = "", // Required error
                ExpirationDuration = 1000, // Range error
                ExpirationUnit = new string('B', 51), // Length error
                CertificateNumberFormat = new string('C', 101) // Length error
            };

            // Act
            var validationResults = ValidateModel(sertifikat);

            // Assert
            validationResults.Should().HaveCountGreaterOrEqualTo(6);
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("TemplateName"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("TemplateDescription"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("ExpirationType"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("ExpirationDuration"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("ExpirationUnit"));
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("CertificateNumberFormat"));

            // Note: PelatihanId validation error is not expected for int types with value 0
            // because 0 is a valid integer value and [Required] on int doesn't work as expected
        }

        [Fact]
        public void PelatihanSertifikat_ExpirationScenarios_ShouldWorkCorrectly()
        {
            // Test multiple expiration scenarios
            var scenarios = new[]
            {
                new { Type = "never", Duration = (int?)null, Expected = "Tidak Ada Kadaluarsa" },
                new { Type = "months", Duration = (int?)6, Expected = "6 bulan" },
                new { Type = "months", Duration = (int?)1, Expected = "1 bulan" },
                new { Type = "years", Duration = (int?)1, Expected = "1 tahun" },
                new { Type = "years", Duration = (int?)5, Expected = "5 tahun" },
                new { Type = "months", Duration = (int?)null, Expected = "Tidak Ditentukan" },
                new { Type = "years", Duration = (int?)null, Expected = "Tidak Ditentukan" }
            };

            foreach (var scenario in scenarios)
            {
                // Arrange
                var sertifikat = new PelatihanSertifikat
                {
                    ExpirationType = scenario.Type,
                    ExpirationDuration = scenario.Duration
                };

                // Act
                var result = sertifikat.GetExpirationDisplayText();

                // Assert
                result.Should().Be(scenario.Expected,
                    $"because ExpirationType='{scenario.Type}' and ExpirationDuration={scenario.Duration} should return '{scenario.Expected}'");
            }
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


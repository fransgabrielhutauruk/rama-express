// RamaExpress.Tests/Services/Admin/PelatihanSertifikatServiceTests.cs
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Services.Admin
{
    public class PelatihanSertifikatServiceTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly PelatihanSertifikatService _service;

        public PelatihanSertifikatServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);
            _service = new PelatihanSertifikatService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetByPelatihanId Tests

        [Fact]
        public async Task GetByPelatihanId_WithValidPelatihanId_ReturnsCertificateWithPelatihan()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetByPelatihanId(1);

            // Assert
            result.Should().NotBeNull();
            result!.PelatihanId.Should().Be(1);
            result.TemplateName.Should().Be("Basic Programming Certificate");
            result.Pelatihan.Should().NotBeNull();
            result.Pelatihan!.Id.Should().Be(1);
        }

        [Fact]
        public async Task GetByPelatihanId_WithInvalidPelatihanId_ReturnsNull()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetByPelatihanId(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByPelatihanId_WithEmptyDatabase_ReturnsNull()
        {
            // Act
            var result = await _service.GetByPelatihanId(1);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithValidId_ReturnsCertificateWithPelatihan()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.TemplateName.Should().Be("Basic Programming Certificate");
            result.Pelatihan.Should().NotBeNull();
            result.Pelatihan!.Judul.Should().Be("Basic Programming");
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Add Tests

        [Fact]
        public async Task Add_WithValidCertificate_ReturnsSuccessAndAddsCertificate()
        {
            // Arrange
            await SeedTestDataWithoutCertificates();
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 1,
                IsSertifikatActive = true,
                TemplateName = "New Certificate Template",
                TemplateDescription = "New certificate description",
                ExpirationType = "months",
                ExpirationDuration = 12,
                ExpirationUnit = "months",
                CertificateNumberFormat = "CERT-{YEAR}-{PELATIHAN_ID}-{INCREMENT}"
            };

            // Act
            var result = await _service.Add(certificate);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Pengaturan sertifikat 'New Certificate Template' berhasil ditambahkan");
            result.Certificate.Should().NotBeNull();
            result.Certificate!.Id.Should().BeGreaterThan(0);
            result.Certificate.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Certificate.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public async Task Add_WithExistingCertificateForPelatihan_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 1, // Already has certificate
                IsSertifikatActive = true,
                TemplateName = "Duplicate Certificate",
                ExpirationType = "never"
            };

            // Act
            var result = await _service.Add(certificate);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Sertifikat untuk pelatihan ini sudah ada");
            result.Certificate.Should().BeNull();
        }

        [Fact]
        public async Task Add_WithNonExistentPelatihan_ReturnsFailure()
        {
            // Arrange
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 999, // Non-existent pelatihan
                IsSertifikatActive = true,
                TemplateName = "Certificate for Non-existent Training",
                ExpirationType = "never"
            };

            // Act
            var result = await _service.Add(certificate);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pelatihan tidak ditemukan");
            result.Certificate.Should().BeNull();
        }

        [Fact]
        public async Task Add_WithDeletedPelatihan_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithDeletedPelatihan();
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 3, // Deleted pelatihan
                IsSertifikatActive = true,
                TemplateName = "Certificate for Deleted Training",
                ExpirationType = "never"
            };

            // Act
            var result = await _service.Add(certificate);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pelatihan tidak ditemukan");
            result.Certificate.Should().BeNull();
        }

        [Fact]
        public async Task Add_WithExpirationTypeButNoDuration_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithoutCertificates();
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 1,
                IsSertifikatActive = true,
                TemplateName = "Certificate with invalid expiration",
                ExpirationType = "months", // Has expiration type
                ExpirationDuration = null, // But no duration
                CertificateNumberFormat = "CERT-{INCREMENT}"
            };

            // Act
            var result = await _service.Add(certificate);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Durasi kadaluarsa wajib diisi untuk jenis kadaluarsa yang dipilih");
            result.Certificate.Should().BeNull();
        }

        [Fact]
        public async Task Add_WithNeverExpirationType_SucceedsWithoutDuration()
        {
            // Arrange
            await SeedTestDataWithoutCertificates();
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 1,
                IsSertifikatActive = true,
                TemplateName = "Never Expires Certificate",
                ExpirationType = "never",
                ExpirationDuration = null,
                CertificateNumberFormat = "CERT-{INCREMENT}"
            };

            // Act
            var result = await _service.Add(certificate);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificate!.ExpirationType.Should().Be("never");
            result.Certificate.ExpirationDuration.Should().BeNull();
        }

        [Fact]
        public async Task Add_NormalizesDataCorrectly()
        {
            // Arrange
            await SeedTestDataWithoutCertificates();
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 1,
                IsSertifikatActive = true,
                TemplateName = "  Template with spaces  ",
                TemplateDescription = "  Description with spaces  ",
                ExpirationType = "never",
                CertificateNumberFormat = "CERT-{INCREMENT}"
            };

            // Act
            var result = await _service.Add(certificate);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificate!.TemplateName.Should().Be("Template with spaces");
            result.Certificate.TemplateDescription.Should().Be("Description with spaces");
        }

        [Fact]
        public async Task Add_WithEmptyDescription_HandlesCorrectly()
        {
            // Arrange
            await SeedTestDataWithoutCertificates();
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 1,
                IsSertifikatActive = true,
                TemplateName = "Certificate without description",
                TemplateDescription = "   ", // Empty/whitespace
                ExpirationType = "never",
                CertificateNumberFormat = "CERT-{INCREMENT}"
            };

            // Act
            var result = await _service.Add(certificate);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificate!.TemplateDescription.Should().BeNullOrEmpty();
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidData_ReturnsSuccessAndUpdatesCertificate()
        {
            // Arrange
            await SeedTestData();
            var updateData = new PelatihanSertifikat
            {
                Id = 1,
                IsSertifikatActive = false,
                TemplateName = "Updated Certificate Template",
                TemplateDescription = "Updated description",
                ExpirationType = "years",
                ExpirationDuration = 2,
                ExpirationUnit = "years",
                CertificateNumberFormat = "UPDATED-{YEAR}-{INCREMENT}"
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Pengaturan sertifikat 'Updated Certificate Template' berhasil diperbarui");
            result.Certificate.Should().NotBeNull();
            result.Certificate!.IsSertifikatActive.Should().BeFalse();
            result.Certificate.TemplateName.Should().Be("Updated Certificate Template");
            result.Certificate.TemplateDescription.Should().Be("Updated description");
            result.Certificate.ExpirationType.Should().Be("years");
            result.Certificate.ExpirationDuration.Should().Be(2);
            result.Certificate.ExpirationUnit.Should().Be("years");
            result.Certificate.CertificateNumberFormat.Should().Be("UPDATED-{YEAR}-{INCREMENT}");
            result.Certificate.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Update_WithNonExistentId_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var updateData = new PelatihanSertifikat
            {
                Id = 999,
                TemplateName = "Non-existent Certificate",
                ExpirationType = "never"
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pengaturan sertifikat tidak ditemukan");
            result.Certificate.Should().BeNull();
        }

        [Fact]
        public async Task Update_WithExpirationTypeButNoDuration_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var updateData = new PelatihanSertifikat
            {
                Id = 1,
                TemplateName = "Updated Certificate",
                ExpirationType = "months",
                ExpirationDuration = null
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Durasi kadaluarsa wajib diisi untuk jenis kadaluarsa yang dipilih");
            result.Certificate.Should().BeNull();
        }

        [Fact]
        public async Task Update_NormalizesDataCorrectly()
        {
            // Arrange
            await SeedTestData();
            var updateData = new PelatihanSertifikat
            {
                Id = 1,
                TemplateName = "  Updated Template  ",
                TemplateDescription = "  Updated Description  ",
                ExpirationType = "never"
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificate!.TemplateName.Should().Be("Updated Template");
            result.Certificate.TemplateDescription.Should().Be("Updated Description");
        }

        [Fact]
        public async Task Update_WithEmptyDescription_SetsToNull()
        {
            // Arrange
            await SeedTestData();
            var updateData = new PelatihanSertifikat
            {
                Id = 1,
                TemplateName = "Updated Certificate",
                TemplateDescription = "",
                ExpirationType = "never"
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.Certificate!.TemplateDescription.Should().BeNull();
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidIdAndNoIssuedCertificates_ReturnsSuccessAndDeletes()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Pengaturan sertifikat 'Basic Programming Certificate' berhasil dihapus");

            // Verify deletion
            var deletedCertificate = await _context.PelatihanSertifikat.FindAsync(1);
            deletedCertificate.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WithNonExistentId_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Delete(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pengaturan sertifikat tidak ditemukan");
        }

        [Fact]
        public async Task Delete_WithIssuedCertificates_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithIssuedCertificates();

            // Act
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Tidak dapat menghapus pengaturan sertifikat karena sudah ada");
            result.Message.Should().Contain("sertifikat yang diterbitkan");
        }

        #endregion

        #region GenerateCertificateNumber Tests

        [Fact]
        public async Task GenerateCertificateNumber_WithValidSettings_GeneratesCorrectFormat()
        {
            // Arrange
            await SeedTestData();
            var currentYear = DateTime.Now.ToString("yyyy");
            var currentMonth = DateTime.Now.ToString("MM");

            // Act
            var result = await _service.GenerateCertificateNumber(1);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith($"CERT-{currentYear}-");
            result.Should().Contain($"{currentMonth}");
            result.Should().EndWith("0001"); // First certificate increment
        }

        [Fact]
        public async Task GenerateCertificateNumber_WithNoSettings_UsesDefaultFormat()
        {
            // Arrange
            await SeedTestDataWithoutCertificates();
            var currentYear = DateTime.Now.ToString("yyyy");
            var currentMonth = DateTime.Now.ToString("MM");

            // Act
            var result = await _service.GenerateCertificateNumber(1);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith($"CERT-{currentYear}-");
            result.Should().Contain($"{currentMonth}");
            result.Should().EndWith("0001");
        }

        [Fact]
        public async Task GenerateCertificateNumber_WithCustomFormat_GeneratesCorrectly()
        {
            // Arrange
            await SeedTestDataWithCustomFormat();
            var currentYear = DateTime.Now.ToString("yyyy");

            // Act
            var result = await _service.GenerateCertificateNumber(2);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith($"CUSTOM-{currentYear}-");
            result.Should().Contain("002"); // Pelatihan ID
            result.Should().EndWith("0001"); // Increment
        }

        [Fact]
        public async Task GenerateCertificateNumber_WithExistingCertificates_IncrementsCorrectly()
        {
            // Arrange
            await SeedTestDataWithIssuedCertificates();

            // Act
            var result = await _service.GenerateCertificateNumber(1);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().EndWith("0003"); // Should be third certificate (2 existing + 1)
        }

        [Fact]
        public async Task GenerateCertificateNumber_WithAllPlaceholders_ReplacesCorrectly()
        {
            // Arrange
            await SeedTestDataWithAllPlaceholders();
            var now = DateTime.Now;

            // Act
            var result = await _service.GenerateCertificateNumber(1);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain(now.ToString("yyyy")); // {YEAR}
            result.Should().Contain(now.ToString("MM"));   // {MONTH}
            result.Should().Contain(now.ToString("dd"));   // {DAY}
            result.Should().Contain("001");                // {PELATIHAN_ID}
            result.Should().Contain("0001");               // {INCREMENT}
        }

        [Fact]
        public async Task GenerateCertificateNumber_WithExceptionInProcessing_ReturnsFallbackFormat()
        {
            // Arrange - Test the fallback behavior when an exception occurs during processing
            // The service should catch exceptions and return a simple fallback format

            // We can't easily simulate a database exception with in-memory database,
            // so we'll test the scenario where no certificate settings exist (which is handled)
            // This tests the actual fallback logic in the service

            var emptyOptions = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var emptyContext = new RamaExpressAppContext(emptyOptions);
            var serviceWithEmptyDb = new PelatihanSertifikatService(emptyContext);

            // Act - This should trigger the fallback since no settings exist
            var result = await serviceWithEmptyDb.GenerateCertificateNumber(1);

            // Assert - Should return fallback format
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("CERT-");
            result.Should().Contain(DateTime.Now.ToString("yyyy"));
            result.Should().Contain(DateTime.Now.ToString("MM"));
            result.Should().EndWith("0001");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_AddUpdateDelete_WorksCorrectly()
        {
            // Arrange
            await SeedTestDataWithoutCertificates();
            var certificate = new PelatihanSertifikat
            {
                PelatihanId = 1,
                IsSertifikatActive = true,
                TemplateName = "Workflow Test Certificate",
                TemplateDescription = "Test workflow description",
                ExpirationType = "months",
                ExpirationDuration = 6,
                ExpirationUnit = "months",
                CertificateNumberFormat = "WORKFLOW-{YEAR}-{INCREMENT}"
            };

            // Act & Assert - Add
            var addResult = await _service.Add(certificate);
            addResult.Success.Should().BeTrue();
            var addedCertificate = addResult.Certificate!;

            // Act & Assert - Update
            addedCertificate.TemplateName = "Updated Workflow Certificate";
            addedCertificate.ExpirationDuration = 12;
            var updateResult = await _service.Update(addedCertificate);
            updateResult.Success.Should().BeTrue();
            updateResult.Certificate!.TemplateName.Should().Be("Updated Workflow Certificate");
            updateResult.Certificate.ExpirationDuration.Should().Be(12);

            // Act & Assert - Generate Certificate Number
            var certificateNumber = await _service.GenerateCertificateNumber(1);
            certificateNumber.Should().StartWith("WORKFLOW-");

            // Act & Assert - Delete
            var deleteResult = await _service.Delete(addedCertificate.Id);
            deleteResult.Success.Should().BeTrue();

            // Verify deletion
            var exists = await _service.ExistsByPelatihanId(1);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task CertificateNumberGeneration_WithMultipleCertificates_IncrementsCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Generate multiple certificate numbers and verify incremental behavior
            var numbers = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                // Generate certificate number first
                var number = await _service.GenerateCertificateNumber(1);
                numbers.Add(number);

                // Then add a new issued certificate for the next iteration
                var sertifikat = new Sertifikat
                {
                    PelatihanId = 1,
                    NomorSertifikat = number // Use the generated number
                };
                _context.Sertifikat.Add(sertifikat);
                await _context.SaveChangesAsync();
            }

            // Assert
            numbers.Should().HaveCount(5);
            numbers[0].Should().EndWith("0001"); // First certificate
            numbers[1].Should().EndWith("0002"); // Second certificate  
            numbers[2].Should().EndWith("0003"); // Third certificate
            numbers[3].Should().EndWith("0004"); // Fourth certificate
            numbers[4].Should().EndWith("0005"); // Fifth certificate
        }

        #endregion

        #region Helper Methods

        private async Task SeedTestData()
        {
            var pelatihans = new List<Pelatihan>
            {
                new()
                {
                    Id = 1,
                    Kode = "PROG001",
                    Judul = "Basic Programming",
                    Deskripsi = "Learn programming fundamentals",
                    DurasiMenit = 120,
                    SkorMinimal = 70,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now.AddDays(-2)
                },
                new()
                {
                    Id = 2,
                    Kode = "WEB001",
                    Judul = "Web Development",
                    Deskripsi = "Learn web development basics",
                    DurasiMenit = 180,
                    SkorMinimal = 75,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now.AddDays(-1)
                }
            };

            var certificates = new List<PelatihanSertifikat>
            {
                new()
                {
                    Id = 1,
                    PelatihanId = 1,
                    IsSertifikatActive = true,
                    TemplateName = "Basic Programming Certificate",
                    TemplateDescription = "Certificate for completing basic programming course",
                    ExpirationType = "months",
                    ExpirationDuration = 12,
                    ExpirationUnit = "months",
                    CertificateNumberFormat = "CERT-{YEAR}-{MONTH}-{INCREMENT}",
                    CreatedAt = DateTime.Now.AddDays(-1)
                }
            };

            _context.Pelatihan.AddRange(pelatihans);
            _context.PelatihanSertifikat.AddRange(certificates);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithoutCertificates()
        {
            var pelatihans = new List<Pelatihan>
            {
                new()
                {
                    Id = 1,
                    Kode = "PROG001",
                    Judul = "Basic Programming",
                    DurasiMenit = 120,
                    SkorMinimal = 70,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now.AddDays(-2)
                }
            };

            _context.Pelatihan.AddRange(pelatihans);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithDeletedPelatihan()
        {
            await SeedTestData();

            var deletedPelatihan = new Pelatihan
            {
                Id = 3,
                Kode = "DEL001",
                Judul = "Deleted Training",
                DurasiMenit = 60,
                SkorMinimal = 70,
                IsActive = true,
                IsDeleted = true, // Deleted
                CreatedAt = DateTime.Now.AddDays(-3),
                DeletedAt = DateTime.Now.AddDays(-1)
            };

            _context.Pelatihan.Add(deletedPelatihan);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithIssuedCertificates()
        {
            await SeedTestData();

            var issuedCertificates = new List<Sertifikat>
            {
                new()
                {
                    PelatihanId = 1,
                    NomorSertifikat = "CERT-2024-01-0001"
                },
                new()
                {
                    PelatihanId = 1,
                    NomorSertifikat = "CERT-2024-01-0002"
                }
            };

            _context.Sertifikat.AddRange(issuedCertificates);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithCustomFormat()
        {
            await SeedTestDataWithoutCertificates();

            var pelatihan2 = new Pelatihan
            {
                Id = 2,
                Kode = "WEB001",
                Judul = "Web Development",
                DurasiMenit = 180,
                SkorMinimal = 75,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now.AddDays(-1)
            };

            var customCertificate = new PelatihanSertifikat
            {
                Id = 2,
                PelatihanId = 2,
                IsSertifikatActive = true,
                TemplateName = "Custom Certificate",
                ExpirationType = "never",
                CertificateNumberFormat = "CUSTOM-{YEAR}-{PELATIHAN_ID}-{INCREMENT}",
                CreatedAt = DateTime.Now
            };

            _context.Pelatihan.Add(pelatihan2);
            _context.PelatihanSertifikat.Add(customCertificate);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithAllPlaceholders()
        {
            await SeedTestDataWithoutCertificates();

            var certificate = new PelatihanSertifikat
            {
                Id = 1,
                PelatihanId = 1,
                IsSertifikatActive = true,
                TemplateName = "Full Format Certificate",
                ExpirationType = "never",
                CertificateNumberFormat = "{YEAR}-{MONTH}-{DAY}-{PELATIHAN_ID}-{INCREMENT}",
                CreatedAt = DateTime.Now
            };

            _context.PelatihanSertifikat.Add(certificate);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}





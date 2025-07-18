// RamaExpress.Tests/Services/Admin/PelatihanServiceTests.cs
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Services.Admin
{
    public class PelatihanServiceTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly PelatihanService _service;

        public PelatihanServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);
            _service = new PelatihanService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            // Remove the _service?.Dispose() call since PelatihanService doesn't implement IDisposable
        }

        #region GetAll Tests

        [Fact]
        public async Task GetAll_WithDefaultParameters_ReturnsFirstPageOfActivePelatihans()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAll();

            // Assert
            result.Pelatihans.Should().HaveCount(2); // Only active, non-deleted
            result.TotalCount.Should().Be(2);
            result.Pelatihans.Should().BeInDescendingOrder(p => p.CreatedAt);
            result.Pelatihans.Should().AllSatisfy(p =>
            {
                p.IsDeleted.Should().BeFalse();
            });
        }

        [Fact]
        public async Task GetAll_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAll(page: 2, pageSize: 1);

            // Assert
            result.Pelatihans.Should().HaveCount(1);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetAll_WithLargePageSize_ReturnsAllItems()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAll(pageSize: 100);

            // Assert
            result.Pelatihans.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetAll_WithEmptyDatabase_ReturnsEmptyResult()
        {
            // Act
            var result = await _service.GetAll();

            // Assert
            result.Pelatihans.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetAllWithSearch Tests

        [Fact]
        public async Task GetAllWithSearch_WithoutFilters_ReturnsAllActivePelatihans()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch();

            // Assert
            result.Pelatihans.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetAllWithSearch_WithJudulSearchTerm_FiltersCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(searchTerm: "Programming");

            // Assert
            result.Pelatihans.Should().HaveCount(1);
            result.Pelatihans.First().Judul.Should().Contain("Programming");
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllWithSearch_WithDeskripsiSearchTerm_FiltersCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(searchTerm: "fundamentals");

            // Assert
            result.Pelatihans.Should().HaveCount(1);
            result.Pelatihans.First().Deskripsi.Should().Contain("fundamentals");
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllWithSearch_WithKodeSearchTerm_FiltersCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(searchTerm: "PROG001");

            // Assert
            result.Pelatihans.Should().HaveCount(1);
            result.Pelatihans.First().Kode.Should().Be("PROG001");
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllWithSearch_WithActiveStatusFilter_FiltersCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(statusFilter: "aktif");

            // Assert
            result.Pelatihans.Should().HaveCount(2);
            result.Pelatihans.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetAllWithSearch_WithInactiveStatusFilter_ReturnsEmpty()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(statusFilter: "tidak aktif");

            // Assert
            result.Pelatihans.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetAllWithSearch_WithSearchTermAndStatusFilter_CombinesFilters()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(searchTerm: "Programming", statusFilter: "aktif");

            // Assert
            result.Pelatihans.Should().HaveCount(1);
            var pelatihan = result.Pelatihans.First();
            pelatihan.Judul.Should().Contain("Programming");
            pelatihan.IsActive.Should().BeTrue();
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllWithSearch_WithNonMatchingSearchTerm_ReturnsEmpty()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(searchTerm: "NonExistent");

            // Assert
            result.Pelatihans.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithValidId_ReturnsPelatihan()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Kode.Should().Be("PROG001");
            result.IsDeleted.Should().BeFalse();
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

        [Fact]
        public async Task GetById_WithDeletedPelatihanId_ReturnsNull()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(3); // Deleted pelatihan

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Add Tests

        [Fact]
        public async Task Add_WithValidPelatihan_ReturnsSuccessAndAddsPelatihan()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "NEW001",
                Judul = "New Training",
                Deskripsi = "New training description",
                DurasiMenit = 120,
                SkorMinimal = 75
            };

            // Act
            var result = await _service.Add(pelatihan);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Pelatihan 'New Training' berhasil ditambahkan");
            result.Pelatihan.Should().NotBeNull();
            result.Pelatihan!.Id.Should().BeGreaterThan(0);
            result.Pelatihan.Kode.Should().Be("NEW001"); // Should be normalized to uppercase
            result.Pelatihan.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Pelatihan.IsActive.Should().BeTrue();
            result.Pelatihan.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task Add_WithDuplicateKode_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var pelatihan = new Pelatihan
            {
                Kode = "PROG001", // Existing code
                Judul = "Duplicate Training",
                DurasiMenit = 60,
                SkorMinimal = 70
            };

            // Act
            var result = await _service.Add(pelatihan);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Kode pelatihan 'PROG001' sudah digunakan");
            result.Pelatihan.Should().BeNull();
        }

        [Fact]
        public async Task Add_NormalizesDataCorrectly()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "  test001  ", // With spaces
                Judul = "  Test Training  ", // With spaces
                Deskripsi = "  Test description  ", // With spaces
                DurasiMenit = 60,
                SkorMinimal = 70
            };

            // Act
            var result = await _service.Add(pelatihan);

            // Assert
            result.Success.Should().BeTrue();
            result.Pelatihan!.Kode.Should().Be("TEST001"); // Trimmed and uppercase
            result.Pelatihan.Judul.Should().Be("Test Training"); // Trimmed
            result.Pelatihan.Deskripsi.Should().Be("Test description"); // Trimmed
        }

        [Fact]
        public async Task Add_WithEmptyDeskripsi_HandlesCorrectly()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "EMPTY001",
                Judul = "Training without description",
                Deskripsi = "   ", // Empty/whitespace
                DurasiMenit = 60,
                SkorMinimal = 70
            };

            // Act
            var result = await _service.Add(pelatihan);

            // Assert
            result.Success.Should().BeTrue();
            result.Pelatihan!.Deskripsi.Should().BeNullOrEmpty();
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidData_ReturnsSuccessAndUpdatesPelatihan()
        {
            // Arrange
            await SeedTestData();
            var updateData = new Pelatihan
            {
                Id = 1,
                Kode = "PROG001_UPDATED",
                Judul = "Updated Programming Training",
                Deskripsi = "Updated description",
                DurasiMenit = 180,
                SkorMinimal = 80,
                IsActive = false
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Pelatihan 'Updated Programming Training' berhasil diperbarui");
            result.Pelatihan.Should().NotBeNull();
            result.Pelatihan!.Kode.Should().Be("PROG001_UPDATED");
            result.Pelatihan.Judul.Should().Be("Updated Programming Training");
            result.Pelatihan.DurasiMenit.Should().Be(180);
            result.Pelatihan.SkorMinimal.Should().Be(80);
            result.Pelatihan.IsActive.Should().BeFalse();
            result.Pelatihan.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Update_WithNonExistentId_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var updateData = new Pelatihan
            {
                Id = 999,
                Kode = "NONEXISTENT001",
                Judul = "Non-existent Training",
                DurasiMenit = 60,
                SkorMinimal = 70
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pelatihan tidak ditemukan");
            result.Pelatihan.Should().BeNull();
        }

        [Fact]
        public async Task Update_WithDuplicateKode_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var updateData = new Pelatihan
            {
                Id = 1,
                Kode = "WEB001", // Existing code from another pelatihan
                Judul = "Updated Training",
                DurasiMenit = 60,
                SkorMinimal = 70
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Kode pelatihan 'WEB001' sudah digunakan oleh pelatihan lain");
            result.Pelatihan.Should().BeNull();
        }

        [Fact]
        public async Task Update_WithSameKode_Succeeds()
        {
            // Arrange
            await SeedTestData();
            var updateData = new Pelatihan
            {
                Id = 1,
                Kode = "PROG001", // Same code as current
                Judul = "Updated Programming Training",
                DurasiMenit = 120,
                SkorMinimal = 75
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.Pelatihan!.Kode.Should().Be("PROG001");
        }

        [Fact]
        public async Task Update_NormalizesDataCorrectly()
        {
            // Arrange
            await SeedTestData();
            var updateData = new Pelatihan
            {
                Id = 1,
                Kode = "  prog001_updated  ",
                Judul = "  Updated Training  ",
                Deskripsi = "  Updated description  ",
                DurasiMenit = 120,
                SkorMinimal = 75
            };

            // Act
            var result = await _service.Update(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.Pelatihan!.Kode.Should().Be("PROG001_UPDATED");
            result.Pelatihan.Judul.Should().Be("Updated Training");
            result.Pelatihan.Deskripsi.Should().Be("Updated description");
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidIdAndNoRelatedData_ReturnsSuccessAndSoftDeletes()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Pelatihan 'Basic Programming' berhasil dihapus");

            // Verify soft delete
            var deletedPelatihan = await _context.Pelatihan.FindAsync(1);
            deletedPelatihan!.IsDeleted.Should().BeTrue();
            deletedPelatihan.DeletedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            deletedPelatihan.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
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
            result.Message.Should().Be("Pelatihan tidak ditemukan");
        }

        [Fact]
        public async Task Delete_WithRelatedMateri_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithRelations();

            // Act
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("tidak dapat dihapus karena memiliki data terkait");
            result.Message.Should().Contain("materi");
        }

        [Fact]
        public async Task Delete_WithRelatedSoal_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithSoal();

            // Act
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("tidak dapat dihapus karena memiliki data terkait");
            result.Message.Should().Contain("soal");
        }

        [Fact]
        public async Task Delete_WithRelatedProgress_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithProgress();

            // Act
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("tidak dapat dihapus karena memiliki data terkait");
            result.Message.Should().Contain("progress");
        }

        #endregion

        #region Exists Tests

        [Fact]
        public async Task Exists_WithValidId_ReturnsTrue()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Exists(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Exists_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Exists(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Exists_WithDeletedPelatihanId_ReturnsFalse()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Exists(3); // Deleted pelatihan

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IsKodeExists Tests

        [Fact]
        public async Task IsKodeExists_WithExistingKode_ReturnsTrue()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsKodeExists("PROG001");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsKodeExists_WithNonExistentKode_ReturnsFalse()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsKodeExists("NONEXISTENT001");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsKodeExists_WithDeletedPelatihanKode_ReturnsFalse()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsKodeExists("DEL001"); // Deleted pelatihan code

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsKodeExists_WithExcludeId_ExcludesCorrectRecord()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsKodeExists("PROG001", excludeId: 1);

            // Assert
            result.Should().BeFalse(); // Should exclude the record with ID 1
        }

        [Fact]
        public async Task IsKodeExists_IsCaseInsensitive()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result1 = await _service.IsKodeExists("prog001");
            var result2 = await _service.IsKodeExists("PROG001");
            var result3 = await _service.IsKodeExists("Prog001");

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeTrue();
        }

        [Fact]
        public async Task IsKodeExists_TrimsWhitespace()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsKodeExists("  PROG001  ");

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_AddUpdateDelete_WorksCorrectly()
        {
            // Arrange
            var pelatihan = new Pelatihan
            {
                Kode = "WORKFLOW001",
                Judul = "Workflow Test Training",
                Deskripsi = "Test workflow description",
                DurasiMenit = 90,
                SkorMinimal = 75
            };

            // Act & Assert - Add
            var addResult = await _service.Add(pelatihan);
            addResult.Success.Should().BeTrue();
            var addedPelatihan = addResult.Pelatihan!;

            // Act & Assert - Update
            addedPelatihan.Judul = "Updated Workflow Training";
            addedPelatihan.DurasiMenit = 120;
            var updateResult = await _service.Update(addedPelatihan);
            updateResult.Success.Should().BeTrue();
            updateResult.Pelatihan!.Judul.Should().Be("Updated Workflow Training");
            updateResult.Pelatihan.DurasiMenit.Should().Be(120);

            // Act & Assert - Delete
            var deleteResult = await _service.Delete(addedPelatihan.Id);
            deleteResult.Success.Should().BeTrue();

            // Verify soft delete
            var exists = await _service.Exists(addedPelatihan.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task SearchAndPagination_WorkTogetherCorrectly()
        {
            // Arrange
            await SeedLargeTestData();

            // Act
            var result = await _service.GetAllWithSearch(
                page: 1,
                pageSize: 2,
                searchTerm: "Test",
                statusFilter: "aktif");

            // Assert
            result.Pelatihans.Should().HaveCount(2);
            result.TotalCount.Should().BeGreaterThan(2);
            result.Pelatihans.Should().AllSatisfy(p =>
            {
                p.Judul.Should().Contain("Test");
                p.IsActive.Should().BeTrue();
                p.IsDeleted.Should().BeFalse();
            });
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
                },
                new()
                {
                    Id = 3,
                    Kode = "DEL001",
                    Judul = "Deleted Training",
                    Deskripsi = "This training is deleted",
                    DurasiMenit = 60,
                    SkorMinimal = 70,
                    IsActive = true,
                    IsDeleted = true, // Deleted
                    CreatedAt = DateTime.Now.AddDays(-3),
                    DeletedAt = DateTime.Now.AddDays(-1)
                }
            };

            _context.Pelatihan.AddRange(pelatihans);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithRelations()
        {
            await SeedTestData();

            var materi = new PelatihanMateri
            {
                Id = 1,
                PelatihanId = 1,
                Judul = "Test Material",
                TipeKonten = "text",
                Konten = "Test content",
                Urutan = 1,
                CreatedAt = DateTime.Now
            };

            _context.PelatihanMateri.Add(materi);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithSoal()
        {
            await SeedTestData();

            var soal = new PelatihanSoal
            {
                Id = 1,
                PelatihanId = 1,
                Pertanyaan = "Test Question?",
                OpsiA = "Option A",
                OpsiB = "Option B",
                OpsiC = "Option C",
                OpsiD = "Option D",
                JawabanBenar = "A",
                Urutan = 1,
                CreatedAt = DateTime.Now
            };

            _context.PelatihanSoal.Add(soal);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithProgress()
        {
            await SeedTestData();

            var progress = new PelatihanProgress
            {
                Id = 1,
                PelatihanId = 1,
                UserId = 1,
                MateriTerakhirId = 1,
                IsCompleted = false,
                StartedAt = DateTime.Now.AddDays(-1)
            };

            _context.PelatihanProgress.Add(progress);
            await _context.SaveChangesAsync();
        }

        private async Task SeedLargeTestData()
        {
            var pelatihans = new List<Pelatihan>();

            for (int i = 1; i <= 10; i++)
            {
                pelatihans.Add(new Pelatihan
                {
                    Id = i,
                    Kode = $"TEST{i:D3}",
                    Judul = $"Test Training {i}",
                    Deskripsi = $"Test description {i}",
                    DurasiMenit = 60 + (i * 10),
                    SkorMinimal = 70,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now.AddDays(-i)
                });
            }

            _context.Pelatihan.AddRange(pelatihans);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}




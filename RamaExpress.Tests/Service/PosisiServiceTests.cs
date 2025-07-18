// RamaExpress.Tests/Services/PosisiServiceTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Services
{
    public class PosisiServiceTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly PosisiService _service;

        public PosisiServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);

            // Create service
            _service = new PosisiService(_context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region GetAll Tests

        [Fact]
        public async Task GetAll_WithActivePosisi_ReturnsOnlyActiveOrderedByName()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAll();

            // Assert
            var posisiList = result.ToList();
            posisiList.Should().HaveCount(3);
            posisiList[0].Name.Should().Be("Developer");
            posisiList[1].Name.Should().Be("Manager");
            posisiList[2].Name.Should().Be("Tester");
            posisiList.Should().OnlyContain(p => !p.IsDeleted);
        }

        [Fact]
        public async Task GetAll_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            // No data seeded

            // Act
            var result = await _service.GetAll();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAll_WithOnlyDeletedPosisi_ReturnsEmptyList()
        {
            // Arrange
            await SeedDeletedPosisiData();

            // Act
            var result = await _service.GetAll();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithValidActiveId_ReturnsPosisi()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Developer");
            result.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task GetById_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetById_WithDeletedId_ReturnsNull()
        {
            // Arrange
            await SeedDeletedPosisiData();

            // Act
            var result = await _service.GetById(4);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Add Tests

        [Fact]
        public async Task Add_WithValidPosisi_AddsSuccessfully()
        {
            // Arrange
            var newPosisi = new Posisi
            {
                Name = "  Product Manager  " // Test trimming
            };

            // Act
            var result = await _service.Add(newPosisi);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Posisi 'Product Manager' berhasil ditambahkan");
            result.Posisi.Should().NotBeNull();
            result.Posisi!.Name.Should().Be("Product Manager"); // Should be trimmed
            result.Posisi.IsDeleted.Should().BeFalse();
            result.Posisi.DeletedAt.Should().BeNull();

            // Verify in database
            var dbPosisi = await _context.Posisi.FindAsync(newPosisi.Id);
            dbPosisi.Should().NotBeNull();
            dbPosisi!.Name.Should().Be("Product Manager");
        }

        [Fact]
        public async Task Add_WithDatabaseError_ReturnsFailure()
        {
            // Arrange
            var posisi = new Posisi { Name = "Test" };
            _context.Dispose(); // Force database error

            // Act
            var result = await _service.Add(posisi);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
            result.Posisi.Should().BeNull();
        }

        [Fact]
        public async Task Add_TrimsWhitespaceFromName()
        {
            // Arrange
            var posisi = new Posisi { Name = "   Business Analyst   " };

            // Act
            var result = await _service.Add(posisi);

            // Assert
            result.Success.Should().BeTrue();
            result.Posisi!.Name.Should().Be("Business Analyst");
        }

        [Fact]
        public async Task Add_SetsDefaultProperties()
        {
            // Arrange
            var posisi = new Posisi
            {
                Name = "QA Engineer",
                IsDeleted = true, // Should be overridden
                DeletedAt = DateTime.Now // Should be overridden
            };

            // Act
            var result = await _service.Add(posisi);

            // Assert
            result.Success.Should().BeTrue();
            result.Posisi!.IsDeleted.Should().BeFalse();
            result.Posisi.DeletedAt.Should().BeNull();
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidPosisi_UpdatesSuccessfully()
        {
            // Arrange
            await SeedTestData();
            var existingPosisi = await _service.GetById(1);
            existingPosisi!.Name = "  Senior Developer  "; // Test trimming

            // Act
            var result = await _service.Update(existingPosisi);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Posisi 'Senior Developer' berhasil diperbarui");
            result.Posisi.Should().NotBeNull();
            result.Posisi!.Name.Should().Be("Senior Developer"); // Should be trimmed

            // Verify in database
            var dbPosisi = await _context.Posisi.FindAsync(1);
            dbPosisi!.Name.Should().Be("Senior Developer");
        }

        [Fact]
        public async Task Update_WithNonExistentPosisi_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var nonExistentPosisi = new Posisi { Id = 999, Name = "Non-existent" };

            // Act
            var result = await _service.Update(nonExistentPosisi);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Posisi tidak ditemukan");
            result.Posisi.Should().BeNull();
        }

        [Fact]
        public async Task Update_WithDeletedPosisi_ReturnsFailure()
        {
            // Arrange
            await SeedDeletedPosisiData();
            var deletedPosisi = new Posisi { Id = 4, Name = "Updated Name" };

            // Act
            var result = await _service.Update(deletedPosisi);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Posisi tidak ditemukan");
            result.Posisi.Should().BeNull();
        }

        [Fact]
        public async Task Update_WithDatabaseError_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var posisi = new Posisi { Id = 1, Name = "Updated" };
            _context.Dispose(); // Force database error

            // Act
            var result = await _service.Update(posisi);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
            result.Posisi.Should().BeNull();
        }

        [Fact]
        public async Task Update_TrimsWhitespaceFromName()
        {
            // Arrange
            await SeedTestData();
            var existingPosisi = await _service.GetById(1);
            existingPosisi!.Name = "   Lead Developer   ";

            // Act
            var result = await _service.Update(existingPosisi);

            // Assert
            result.Success.Should().BeTrue();
            result.Posisi!.Name.Should().Be("Lead Developer");
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidIdAndNoEmployees_DeletesSuccessfully()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Delete(3); // Manager position with no employees

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Posisi 'Manager' berhasil dihapus");

            // Verify soft delete in database
            var dbPosisi = await _context.Posisi.FindAsync(3);
            dbPosisi!.IsDeleted.Should().BeTrue();
            dbPosisi.DeletedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Delete_WithEmployeesInPosition_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithEmployees();

            // Act
            var result = await _service.Delete(1); // Developer position with 2 employees

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Posisi 'Developer' tidak dapat dihapus karena masih digunakan oleh 2 karyawan");

            // Verify not deleted in database
            var dbPosisi = await _context.Posisi.FindAsync(1);
            dbPosisi!.IsDeleted.Should().BeFalse();
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
            result.Message.Should().Be("Posisi tidak ditemukan");
        }

        [Fact]
        public async Task Delete_WithAlreadyDeletedPosisi_ReturnsFailure()
        {
            // Arrange
            await SeedDeletedPosisiData();

            // Act
            var result = await _service.Delete(4);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Posisi tidak ditemukan");
        }

        [Fact]
        public async Task Delete_WithDatabaseError_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            _context.Dispose(); // Force database error

            // Act
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
        }

        [Fact]
        public async Task Delete_OnlyCountsActiveEmployees()
        {
            // Arrange
            await SeedTestDataWithActiveAndInactiveEmployees();

            // Act - Developer position has 1 active and 1 deleted employee
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Posisi 'Developer' tidak dapat dihapus karena masih digunakan oleh 1 karyawan");
        }

        #endregion

        #region GetActivePosisi Tests

        [Fact]
        public async Task GetActivePosisi_WithActivePosisi_ReturnsOnlyActiveOrderedByName()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetActivePosisi();

            // Assert
            var posisiList = result.ToList();
            posisiList.Should().HaveCount(3);
            posisiList[0].Name.Should().Be("Developer");
            posisiList[1].Name.Should().Be("Manager");
            posisiList[2].Name.Should().Be("Tester");
            posisiList.Should().OnlyContain(p => !p.IsDeleted);
        }

        [Fact]
        public async Task GetActivePosisi_WithMixedActiveAndDeleted_ReturnsOnlyActive()
        {
            // Arrange
            await SeedTestData();
            await SeedDeletedPosisiData();

            // Act
            var result = await _service.GetActivePosisi();

            // Assert
            var posisiList = result.ToList();
            posisiList.Should().HaveCount(3); // Only active ones
            posisiList.Should().OnlyContain(p => !p.IsDeleted);
        }

        #endregion

        #region GetPosisiWithEmployeeCount Tests

        [Fact]
        public async Task GetPosisiWithEmployeeCount_WithEmployees_ReturnsCorrectCounts()
        {
            // Arrange
            await SeedTestDataWithEmployees();

            // Act
            var result = await _service.GetPosisiWithEmployeeCount();

            // Assert
            var posisiList = result.ToList();
            posisiList.Should().HaveCount(3);

            var developer = posisiList.First(p => p.Name == "Developer");
            developer.EmployeeCount.Should().Be(2);

            var tester = posisiList.First(p => p.Name == "Tester");
            tester.EmployeeCount.Should().Be(1);

            var manager = posisiList.First(p => p.Name == "Manager");
            manager.EmployeeCount.Should().Be(0);
        }

        [Fact]
        public async Task GetPosisiWithEmployeeCount_WithNoEmployees_ReturnsZeroCounts()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetPosisiWithEmployeeCount();

            // Assert
            var posisiList = result.ToList();
            posisiList.Should().HaveCount(3);
            posisiList.Should().OnlyContain(p => p.EmployeeCount == 0);
        }

        [Fact]
        public async Task GetPosisiWithEmployeeCount_OnlyCountsActiveEmployees()
        {
            // Arrange
            await SeedTestDataWithActiveAndInactiveEmployees();

            // Act
            var result = await _service.GetPosisiWithEmployeeCount();

            // Assert
            var posisiList = result.ToList();
            var developer = posisiList.First(p => p.Name == "Developer");
            developer.EmployeeCount.Should().Be(1); // Only active employee counted
        }

        [Fact]
        public async Task GetPosisiWithEmployeeCount_ReturnsCorrectViewModelProperties()
        {
            // Arrange
            await SeedTestDataWithEmployees();

            // Act
            var result = await _service.GetPosisiWithEmployeeCount();

            // Assert
            var posisiList = result.ToList();
            var developer = posisiList.First(p => p.Name == "Developer");

            developer.Id.Should().Be(1);
            developer.Name.Should().Be("Developer");
            developer.IsDeleted.Should().BeFalse();
            developer.DeletedAt.Should().BeNull();
            developer.EmployeeCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPosisiWithEmployeeCount_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            // No data seeded

            // Act
            var result = await _service.GetPosisiWithEmployeeCount();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_AddUpdateDelete_WorksCorrectly()
        {
            // Arrange & Act 1: Add
            var newPosisi = new Posisi { Name = "DevOps Engineer" };
            var addResult = await _service.Add(newPosisi);
            addResult.Success.Should().BeTrue();

            // Act 2: Update
            addResult.Posisi!.Name = "Senior DevOps Engineer";
            var updateResult = await _service.Update(addResult.Posisi);
            updateResult.Success.Should().BeTrue();

            // Act 3: Get by ID
            var getResult = await _service.GetById(addResult.Posisi.Id);
            getResult.Should().NotBeNull();
            getResult!.Name.Should().Be("Senior DevOps Engineer");

            // Act 4: Delete
            var deleteResult = await _service.Delete(addResult.Posisi.Id);
            deleteResult.Success.Should().BeTrue();

            // Act 5: Verify deleted
            var deletedPosisi = await _service.GetById(addResult.Posisi.Id);
            deletedPosisi.Should().BeNull();
        }

        [Fact]
        public async Task GetAll_And_GetActivePosisi_ReturnSameResults()
        {
            // Arrange
            await SeedTestData();

            // Act
            var getAllResult = await _service.GetAll();
            var getActiveResult = await _service.GetActivePosisi();

            // Assert
            getAllResult.Should().BeEquivalentTo(getActiveResult);
        }

        #endregion

        #region Helper Methods

        private async Task SeedTestData()
        {
            var posisiList = new List<Posisi>
            {
                new() { Id = 1, Name = "Developer", IsDeleted = false, DeletedAt = null },
                new() { Id = 2, Name = "Tester", IsDeleted = false, DeletedAt = null },
                new() { Id = 3, Name = "Manager", IsDeleted = false, DeletedAt = null }
            };

            _context.Posisi.AddRange(posisiList);
            await _context.SaveChangesAsync();
        }

        private async Task SeedDeletedPosisiData()
        {
            var deletedPosisi = new Posisi
            {
                Id = 4,
                Name = "Deleted Position",
                IsDeleted = true,
                DeletedAt = DateTime.Now.AddDays(-1)
            };

            _context.Posisi.Add(deletedPosisi);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithEmployees()
        {
            await SeedTestData();

            var users = new List<User>
            {
                new() { Id = 1, Nama = "John Doe", Email = "john@test.com", Posisi = "Developer", Role = "karyawan", IsDeleted = false },
                new() { Id = 2, Nama = "Jane Smith", Email = "jane@test.com", Posisi = "Developer", Role = "karyawan", IsDeleted = false },
                new() { Id = 3, Nama = "Bob Wilson", Email = "bob@test.com", Posisi = "Tester", Role = "karyawan", IsDeleted = false }
            };

            _context.User.AddRange(users);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithActiveAndInactiveEmployees()
        {
            await SeedTestData();

            var users = new List<User>
            {
                new() { Id = 1, Nama = "Active Dev", Email = "active@test.com", Posisi = "Developer", Role = "karyawan", IsDeleted = false },
                new() { Id = 2, Nama = "Deleted Dev", Email = "deleted@test.com", Posisi = "Developer", Role = "karyawan", IsDeleted = true }
            };

            _context.User.AddRange(users);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}



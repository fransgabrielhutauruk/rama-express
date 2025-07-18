// RamaExpress.Tests/Services/Admin/KaryawanServiceTests.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Services.Admin
{
    public class KaryawanServiceTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly KaryawanService _service;

        public KaryawanServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);
            _service = new KaryawanService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region Add Tests (Legacy Method)

        [Fact]
        public async Task Add_WithValidUser_AddsUserToDatabase()
        {
            // Arrange
            var user = new User
            {
                Nama = "John Doe",
                Email = "john@test.com",
                Posisi = "Developer",
                Role = "karyawan"
            };

            // Act
            await _service.Add(user);

            // Assert
            var addedUser = await _context.User.FirstOrDefaultAsync(u => u.Email == "john@test.com");
            addedUser.Should().NotBeNull();
            addedUser!.Nama.Should().Be("John Doe");
            addedUser.Email.Should().Be("john@test.com");
        }

        #endregion

        #region GetAll Tests

        [Fact]
        public async Task GetAll_WithDefaultParameters_ReturnsKaryawanUsers()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAll();

            // Assert
            result.Users.Should().HaveCount(2); // Only active karyawan users
            result.TotalCount.Should().Be(2);
            result.Users.Should().BeInAscendingOrder(u => u.Nama);
            result.Users.Should().AllSatisfy(u =>
            {
                u.Role.Should().Be("karyawan");
                u.IsDeleted.Should().BeFalse();
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
            result.Users.Should().HaveCount(1);
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
            result.Users.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetAll_WithEmptyDatabase_ReturnsEmptyResult()
        {
            // Act
            var result = await _service.GetAll();

            // Assert
            result.Users.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetAllWithSearch Tests

        [Fact]
        public async Task GetAllWithSearch_WithoutFilters_ReturnsAllKaryawan()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch();

            // Assert
            result.Users.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetAllWithSearch_WithNameSearchTerm_FiltersCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(searchTerm: "John");

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().Nama.Should().Contain("John");
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllWithSearch_WithEmailSearchTerm_FiltersCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(searchTerm: "jane@test.com");

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().Email.Should().Contain("jane@test.com");
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
            result.Users.Should().HaveCount(2);
            result.Users.Should().AllSatisfy(u => u.IsActive.Should().BeTrue());
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
            result.Users.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetAllWithSearch_WithSearchTermAndStatusFilter_CombinesFilters()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearch(searchTerm: "John", statusFilter: "aktif");

            // Assert
            result.Users.Should().HaveCount(1);
            var user = result.Users.First();
            user.Nama.Should().Contain("John");
            user.IsActive.Should().BeTrue();
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
            result.Users.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetAllWithSearchAndSort Tests

        [Fact]
        public async Task GetAllWithSearchAndSort_WithDefaultSort_SortsByNameAscending()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort();

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().BeInAscendingOrder(u => u.Nama);
        }

        [Fact]
        public async Task GetAllWithSearchAndSort_WithNameDescending_SortsCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort(sortField: "Nama", sortDirection: "desc");

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().BeInDescendingOrder(u => u.Nama);
        }

        [Fact]
        public async Task GetAllWithSearchAndSort_WithEmailSort_SortsCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort(sortField: "Email", sortDirection: "asc");

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().BeInAscendingOrder(u => u.Email);
        }

        [Fact]
        public async Task GetAllWithSearchAndSort_WithPosisiSort_SortsCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort(sortField: "Posisi", sortDirection: "asc");

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().BeInAscendingOrder(u => u.Posisi);
        }

        [Fact]
        public async Task GetAllWithSearchAndSort_WithCreatedAtSort_SortsCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort(sortField: "CreatedAt", sortDirection: "desc");

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().BeInDescendingOrder(u => u.CreatedAt);
        }

        [Fact]
        public async Task GetAllWithSearchAndSort_WithIsActiveSort_SortsCorrectly()
        {
            // Arrange
            await SeedTestDataWithInactiveUsers();

            // Act
            var result = await _service.GetAllWithSearchAndSort(sortField: "IsActive", sortDirection: "desc");

            // Assert
            result.Users.Should().HaveCount(3);
            result.Users.Should().BeInDescendingOrder(u => u.IsActive);
        }

        [Fact]
        public async Task GetAllWithSearchAndSort_WithInvalidSortField_DefaultsToName()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort(sortField: "InvalidField");

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().BeInAscendingOrder(u => u.Nama);
        }

        [Fact]
        public async Task GetAllWithSearchAndSort_WithInvalidSortDirection_DefaultsToAscending()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort(sortDirection: "invalid");

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().BeInAscendingOrder(u => u.Nama);
        }

        [Fact]
        public async Task GetAllWithSearchAndSort_WithPosisiSearchTerm_FiltersCorrectly()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort(searchTerm: "Developer");

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().Posisi.Should().Contain("Developer");
        }

        #endregion

        #region AddKaryawan Tests

        [Fact]
        public async Task AddKaryawan_WithValidData_ReturnsSuccessAndAddsUser()
        {
            // Arrange
            var user = new User
            {
                Nama = "New Employee",
                Email = "new@test.com",
                Password = "password123",
                Posisi = "Developer"
            };

            // Act
            var result = await _service.AddKaryawan(user);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Karyawan New Employee berhasil ditambahkan");
            result.User.Should().NotBeNull();
            result.User!.Id.Should().BeGreaterThan(0);
            result.User.Role.Should().Be("karyawan");
            result.User.Email.Should().Be("new@test.com"); // Should be normalized to lowercase
            result.User.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.User.IsActive.Should().BeTrue();
            result.User.IsDeleted.Should().BeFalse();

            // Verify password is hashed
            result.User.Password.Should().NotBe("password123");
            result.User.Password.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task AddKaryawan_WithDuplicateEmail_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var user = new User
            {
                Nama = "Duplicate User",
                Email = "john@test.com", // Existing email
                Password = "password123",
                Posisi = "Developer"
            };

            // Act
            var result = await _service.AddKaryawan(user);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Email sudah digunakan oleh pengguna lain");
            result.User.Should().BeNull();
        }

        [Fact]
        public async Task AddKaryawan_NormalizesDataCorrectly()
        {
            // Arrange
            var user = new User
            {
                Nama = "  Test User  ", // With spaces
                Email = "  TEST@EXAMPLE.COM  ", // With spaces and uppercase
                Password = "password123",
                Posisi = "  Developer  " // With spaces
            };

            // Act
            var result = await _service.AddKaryawan(user);

            // Assert
            result.Success.Should().BeTrue();
            result.User!.Nama.Should().Be("Test User"); // Trimmed
            result.User.Email.Should().Be("test@example.com"); // Trimmed and lowercase
            result.User.Posisi.Should().Be("Developer"); // Trimmed
        }

        [Fact]
        public async Task AddKaryawan_WithEmptyPosisi_HandlesCorrectly()
        {
            // Arrange
            var user = new User
            {
                Nama = "Test User",
                Email = "test@test.com",
                Password = "password123",
                Posisi = "   " // Empty/whitespace
            };

            // Act
            var result = await _service.AddKaryawan(user);

            // Assert
            result.Success.Should().BeTrue();
            result.User!.Posisi.Should().BeNullOrEmpty();
        }

        #endregion

        #region IsEmailExists Tests

        [Fact]
        public async Task IsEmailExists_WithExistingEmail_ReturnsTrue()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsEmailExists("john@test.com");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsEmailExists_WithNonExistentEmail_ReturnsFalse()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsEmailExists("nonexistent@test.com");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsEmailExists_WithDeletedUserEmail_ReturnsFalse()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsEmailExists("deleted@test.com"); // Deleted user email

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsEmailExists_WithExcludeId_ExcludesCorrectRecord()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsEmailExists("john@test.com", excludeId: 1);

            // Assert
            result.Should().BeFalse(); // Should exclude the record with ID 1
        }

        [Fact]
        public async Task IsEmailExists_IsCaseInsensitive()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result1 = await _service.IsEmailExists("JOHN@TEST.COM");
            var result2 = await _service.IsEmailExists("John@Test.Com");

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }

        [Fact]
        public async Task IsEmailExists_TrimsWhitespace()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.IsEmailExists("  john@test.com  ");

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithValidId_ReturnsUser()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Nama.Should().Be("John Doe");
            result.Role.Should().Be("karyawan");
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
        public async Task GetById_WithDeletedUserId_ReturnsNull()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(3); // Deleted user

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetById_WithAdminUserId_ReturnsNull()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(4); // Admin user

            // Assert
            result.Should().BeNull(); // Should only return karyawan role
        }

        #endregion

        #region UpdateKaryawan Tests

        [Fact]
        public async Task UpdateKaryawan_WithValidData_ReturnsSuccessAndUpdatesUser()
        {
            // Arrange
            await SeedTestData();
            var updateData = new User
            {
                Id = 1,
                Nama = "John Doe Updated",
                Email = "john.updated@test.com",
                Posisi = "Senior Developer",
                Password = "newpassword123"
            };

            // Act
            var result = await _service.UpdateKaryawan(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Data karyawan John Doe Updated berhasil diperbarui");
            result.User.Should().NotBeNull();
            result.User!.Nama.Should().Be("John Doe Updated");
            result.User.Email.Should().Be("john.updated@test.com");
            result.User.Posisi.Should().Be("Senior Developer");
            result.User.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));

            // Verify password was updated and hashed
            result.User.Password.Should().NotBe("newpassword123");
            result.User.Password.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task UpdateKaryawan_WithNonExistentId_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var updateData = new User
            {
                Id = 999,
                Nama = "Non-existent User",
                Email = "nonexistent@test.com"
            };

            // Act
            var result = await _service.UpdateKaryawan(updateData);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Karyawan tidak ditemukan");
            result.User.Should().BeNull();
        }

        [Fact]
        public async Task UpdateKaryawan_WithDuplicateEmail_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var updateData = new User
            {
                Id = 1,
                Nama = "John Doe",
                Email = "jane@test.com", // Existing email from another user
                Posisi = "Developer"
            };

            // Act
            var result = await _service.UpdateKaryawan(updateData);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Email sudah digunakan oleh pengguna lain");
            result.User.Should().BeNull();
        }

        [Fact]
        public async Task UpdateKaryawan_WithSameEmail_Succeeds()
        {
            // Arrange
            await SeedTestData();
            var updateData = new User
            {
                Id = 1,
                Nama = "John Doe Updated",
                Email = "john@test.com", // Same email as current
                Posisi = "Senior Developer"
            };

            // Act
            var result = await _service.UpdateKaryawan(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.User!.Email.Should().Be("john@test.com");
        }

        [Fact]
        public async Task UpdateKaryawan_WithEmptyPassword_DoesNotUpdatePassword()
        {
            // Arrange
            await SeedTestData();
            var originalUser = await _service.GetById(1);
            var originalPassword = originalUser!.Password;

            var updateData = new User
            {
                Id = 1,
                Nama = "John Doe Updated",
                Email = "john@test.com",
                Password = "", // Empty password
                Posisi = "Senior Developer"
            };

            // Act
            var result = await _service.UpdateKaryawan(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.User!.Password.Should().Be(originalPassword); // Password should remain unchanged
        }

        [Fact]
        public async Task UpdateKaryawan_NormalizesDataCorrectly()
        {
            // Arrange
            await SeedTestData();
            var updateData = new User
            {
                Id = 1,
                Nama = "  John Updated  ",
                Email = "  JOHN.UPDATED@TEST.COM  ",
                Posisi = "  Senior Developer  "
            };

            // Act
            var result = await _service.UpdateKaryawan(updateData);

            // Assert
            result.Success.Should().BeTrue();
            result.User!.Nama.Should().Be("John Updated");
            result.User.Email.Should().Be("john.updated@test.com");
            result.User.Posisi.Should().Be("Senior Developer");
        }

        #endregion

        #region DeleteKaryawan Tests

        [Fact]
        public async Task DeleteKaryawan_WithValidId_ReturnsSuccessAndSoftDeletes()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.DeleteKaryawan(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Karyawan John Doe berhasil dihapus");

            // Verify soft delete
            var deletedUser = await _context.User.FindAsync(1);
            deletedUser!.IsDeleted.Should().BeTrue();
            deletedUser.DeletedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            deletedUser.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task DeleteKaryawan_WithNonExistentId_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.DeleteKaryawan(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Karyawan tidak ditemukan");
        }

        #endregion

        #region ToggleActiveStatus Tests

        [Fact]
        public async Task ToggleActiveStatus_WithActiveUser_DeactivatesUser()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.ToggleActiveStatus(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Karyawan John Doe berhasil dinonaktifkan");

            // Verify status change
            var user = await _context.User.FindAsync(1);
            user!.IsActive.Should().BeFalse();
            user.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ToggleActiveStatus_WithInactiveUser_ActivatesUser()
        {
            // Arrange
            await SeedTestDataWithInactiveUsers();

            // Act
            var result = await _service.ToggleActiveStatus(5); // Inactive user

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Karyawan Inactive User berhasil diaktifkan");

            // Verify status change
            var user = await _context.User.FindAsync(5);
            user!.IsActive.Should().BeTrue();
            user.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ToggleActiveStatus_WithNonExistentId_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.ToggleActiveStatus(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Karyawan tidak ditemukan");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_AddUpdateDeleteToggle_WorksCorrectly()
        {
            // Arrange
            var user = new User
            {
                Nama = "Workflow Test User",
                Email = "workflow@test.com",
                Password = "password123",
                Posisi = "Developer"
            };

            // Act & Assert - Add
            var addResult = await _service.AddKaryawan(user);
            addResult.Success.Should().BeTrue();
            var addedUser = addResult.User!;

            // Act & Assert - Update
            addedUser.Nama = "Updated Workflow User";
            addedUser.Posisi = "Senior Developer";
            var updateResult = await _service.UpdateKaryawan(addedUser);
            updateResult.Success.Should().BeTrue();
            updateResult.User!.Nama.Should().Be("Updated Workflow User");
            updateResult.User.Posisi.Should().Be("Senior Developer");

            // Act & Assert - Toggle Status
            var toggleResult = await _service.ToggleActiveStatus(addedUser.Id);
            toggleResult.Success.Should().BeTrue();
            toggleResult.Message.Should().Contain("dinonaktifkan");

            // Act & Assert - Delete
            var deleteResult = await _service.DeleteKaryawan(addedUser.Id);
            deleteResult.Success.Should().BeTrue();

            // Verify soft delete
            var deletedUser = await _context.User.FindAsync(addedUser.Id);
            deletedUser!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task SearchSortAndPagination_WorkTogetherCorrectly()
        {
            // Arrange
            await SeedLargeTestData();

            // Act
            var result = await _service.GetAllWithSearchAndSort(
                page: 1,
                pageSize: 3,
                searchTerm: "User",
                statusFilter: "aktif",
                sortField: "Nama",
                sortDirection: "desc");

            // Assert
            result.Users.Should().HaveCount(3);
            result.TotalCount.Should().BeGreaterThan(3);
            result.Users.Should().AllSatisfy(u =>
            {
                u.Nama.Should().Contain("User");
                u.IsActive.Should().BeTrue();
                u.IsDeleted.Should().BeFalse();
                u.Role.Should().Be("karyawan");
            });
            result.Users.Should().BeInDescendingOrder(u => u.Nama);
        }

        #endregion

        #region Helper Methods

        private async Task SeedTestData()
        {
            var users = new List<User>
            {
                new()
                {
                    Id = 1,
                    Nama = "John Doe",
                    Email = "john@test.com",
                    Posisi = "Developer",
                    Role = "karyawan",
                    Password = "hashedpassword1",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    IsActive = true,
                    IsDeleted = false
                },
                new()
                {
                    Id = 2,
                    Nama = "Jane Smith",
                    Email = "jane@test.com",
                    Posisi = "Tester",
                    Role = "karyawan",
                    Password = "hashedpassword2",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    IsActive = true,
                    IsDeleted = false
                },
                new()
                {
                    Id = 3,
                    Nama = "Deleted User",
                    Email = "deleted@test.com",
                    Posisi = "Designer",
                    Role = "karyawan",
                    Password = "hashedpassword3",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    IsActive = true,
                    IsDeleted = true, // Deleted
                    DeletedAt = DateTime.Now.AddDays(-1)
                },
                new()
                {
                    Id = 4,
                    Nama = "Admin User",
                    Email = "admin@test.com",
                    Posisi = "Manager",
                    Role = "admin", // Not karyawan
                    Password = "hashedpassword4",
                    CreatedAt = DateTime.Now.AddDays(-4),
                    IsActive = true,
                    IsDeleted = false
                }
            };

            _context.User.AddRange(users);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithInactiveUsers()
        {
            await SeedTestData();

            var inactiveUser = new User
            {
                Id = 5,
                Nama = "Inactive User",
                Email = "inactive@test.com",
                Posisi = "Developer",
                Role = "karyawan",
                Password = "hashedpassword5",
                CreatedAt = DateTime.Now.AddDays(-1),
                IsActive = false, // Inactive
                IsDeleted = false
            };

            _context.User.Add(inactiveUser);
            await _context.SaveChangesAsync();
        }

        private async Task SeedLargeTestData()
        {
            var users = new List<User>();

            for (int i = 1; i <= 10; i++)
            {
                users.Add(new User
                {
                    Id = i,
                    Nama = $"Test User {i}",
                    Email = $"user{i}@test.com",
                    Posisi = i % 2 == 0 ? "Developer" : "Tester",
                    Role = "karyawan",
                    Password = $"hashedpassword{i}",
                    CreatedAt = DateTime.Now.AddDays(-i),
                    IsActive = true,
                    IsDeleted = false
                });
            }

            _context.User.AddRange(users);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
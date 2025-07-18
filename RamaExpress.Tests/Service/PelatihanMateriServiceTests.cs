// RamaExpress.Tests/Services/PelatihanMateriServiceTests.cs
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Services
{
    public class PelatihanMateriServiceTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly PelatihanMateriService _service;

        public PelatihanMateriServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);

            // Create service
            _service = new PelatihanMateriService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetByPelatihanId Tests

        [Fact]
        public async Task GetByPelatihanId_WithValidPelatihanId_ReturnsMaterialsInOrder()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetByPelatihanId(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            var materials = result.ToList();
            materials[0].Urutan.Should().Be(1);
            materials[1].Urutan.Should().Be(2);
            materials[2].Urutan.Should().Be(3);
            materials[0].Judul.Should().Be("Material 1");
            materials[1].Judul.Should().Be("Material 2");
            materials[2].Judul.Should().Be("Material 3");
        }

        [Fact]
        public async Task GetByPelatihanId_WithNonExistentPelatihanId_ReturnsEmptyList()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetByPelatihanId(999);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByPelatihanId_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            // No test data seeded

            // Act
            var result = await _service.GetByPelatihanId(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithValidId_ReturnsMaterialWithPelatihan()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Judul.Should().Be("Material 1");
            result.Pelatihan.Should().NotBeNull();
            result.Pelatihan!.Judul.Should().Be("Training 1");
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

        #endregion

        #region Add Tests

        [Fact]
        public async Task Add_WithValidMaterial_ReturnsSuccessAndAddsMaterial()
        {
            // Arrange
            await SeedTestData();
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "New Material",
                TipeKonten = "text",
                Konten = "New content for the material"
            };

            // Act
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Materi 'New Material' berhasil ditambahkan");
            result.Materi.Should().NotBeNull();
            result.Materi!.Urutan.Should().Be(4);
            result.Materi.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
            result.Materi.UpdatedAt.Should().BeNull();

            // Verify in database
            var savedMaterial = await _context.PelatihanMateri.FindAsync(result.Materi.Id);
            savedMaterial.Should().NotBeNull();
            savedMaterial!.Judul.Should().Be("New Material");
            savedMaterial.Konten.Should().Be("New content for the material");
        }

        [Fact]
        public async Task Add_WithNonExistentPelatihan_ReturnsFailure()
        {
            // Arrange
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 999,
                Judul = "New Material",
                TipeKonten = "text",
                Konten = "Content"
            };

            // Act
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pelatihan tidak ditemukan");
            result.Materi.Should().BeNull();
        }

        [Fact]
        public async Task Add_WithDeletedPelatihan_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithDeletedPelatihan();
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 2,
                Judul = "New Material",
                TipeKonten = "text",
                Konten = "Content"
            };

            // Act
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pelatihan tidak ditemukan");
            result.Materi.Should().BeNull();
        }

        [Fact]
        public async Task Add_TrimsWhitespaceFromFields()
        {
            // Arrange
            await SeedTestData();
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "  New Material  ",
                TipeKonten = "text",
                Konten = "  Content with spaces  "
            };

            // Act
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeTrue();
            result.Materi!.Judul.Should().Be("New Material");
            result.Materi.Konten.Should().Be("Content with spaces");
        }

        [Fact]
        public async Task Add_WithEmptyKonten_HandlesGracefully()
        {
            // Arrange
            await SeedTestData();
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "New Material",
                TipeKonten = "text",
                Konten = ""
            };

            // Act
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeTrue();
            result.Materi!.Konten.Should().Be("");
        }

        // RamaExpress.Tests/Services/PelatihanMateriServiceTests.cs - CORRECTED VERSION

        [Fact]
        public async Task Add_WithNullKonten_FailsBecauseKontenIsRequired()
        {
            // Arrange
            await SeedTestData();
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "New Material",
                TipeKonten = "text",
                Konten = null! // This violates the [Required] attribute
            };

            // Act
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
            result.Materi.Should().BeNull();
        }

        [Fact]
        public async Task Add_SetsCorrectOrderNumber()
        {
            // Arrange
            await SeedTestData();

            // Act
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "Fourth Material",
                TipeKonten = "text",
                Konten = "Content"
            };
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeTrue();
            result.Materi!.Urutan.Should().Be(4); // Should be after existing 3 materials
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidMaterial_ReturnsSuccessAndUpdatesMaterial()
        {
            // Arrange
            await SeedTestData();
            var updatedMaterial = new PelatihanMateri
            {
                Id = 1,
                Judul = "Updated Material",
                TipeKonten = "video",
                Konten = "Updated content"
            };

            // Act
            var result = await _service.Update(updatedMaterial);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Materi 'Updated Material' berhasil diperbarui");
            result.Materi.Should().NotBeNull();

            // Verify in database
            var savedMaterial = await _context.PelatihanMateri.FindAsync(1);
            savedMaterial!.Judul.Should().Be("Updated Material");
            savedMaterial.TipeKonten.Should().Be("video");
            savedMaterial.Konten.Should().Be("Updated content");
            savedMaterial.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Update_WithNonExistentMaterial_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var updatedMaterial = new PelatihanMateri
            {
                Id = 999,
                Judul = "Updated Material",
                TipeKonten = "text",
                Konten = "Content"
            };

            // Act
            var result = await _service.Update(updatedMaterial);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Materi tidak ditemukan");
            result.Materi.Should().BeNull();
        }

        [Fact]
        public async Task Update_TrimsWhitespaceFromFields()
        {
            // Arrange
            await SeedTestData();
            var updatedMaterial = new PelatihanMateri
            {
                Id = 1,
                Judul = "  Updated Material  ",
                TipeKonten = "text",
                Konten = "  Updated content  "
            };

            // Act
            var result = await _service.Update(updatedMaterial);

            // Assert
            result.Success.Should().BeTrue();

            var savedMaterial = await _context.PelatihanMateri.FindAsync(1);
            savedMaterial!.Judul.Should().Be("Updated Material");
            savedMaterial.Konten.Should().Be("Updated content");
        }

        [Fact]
        public async Task Update_WithEmptyKonten_SetsToNull()
        {
            // Arrange
            await SeedTestData();
            var updatedMaterial = new PelatihanMateri
            {
                Id = 1,
                Judul = "Updated Material",
                TipeKonten = "text",
                Konten = ""
            };

            // Act
            var result = await _service.Update(updatedMaterial);

            // Assert
            result.Success.Should().BeTrue();

            var savedMaterial = await _context.PelatihanMateri.FindAsync(1);
            savedMaterial!.Konten.Should().BeNull();
        }

        [Fact]
        public async Task Update_WithNullKonten_SetsToNull()
        {
            // Arrange
            await SeedTestData();
            var updatedMaterial = new PelatihanMateri
            {
                Id = 1,
                Judul = "Updated Material",
                TipeKonten = "text",
                Konten = null!
            };

            // Act
            var result = await _service.Update(updatedMaterial);

            // Assert
            result.Success.Should().BeTrue();

            var savedMaterial = await _context.PelatihanMateri.FindAsync(1);
            savedMaterial!.Konten.Should().BeNull();
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidId_ReturnsSuccessAndDeletesMaterial()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Delete(2); // Delete middle material

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Materi 'Material 2' berhasil dihapus");

            // Verify material is deleted
            var deletedMaterial = await _context.PelatihanMateri.FindAsync(2);
            deletedMaterial.Should().BeNull();

            // Verify remaining materials are reordered
            var remainingMaterials = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == 1)
                .OrderBy(m => m.Urutan)
                .ToListAsync();

            remainingMaterials.Should().HaveCount(2);
            remainingMaterials[0].Urutan.Should().Be(1);
            remainingMaterials[1].Urutan.Should().Be(2);
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
            result.Message.Should().Be("Materi tidak ditemukan");
        }

        [Fact]
        public async Task Delete_LastMaterial_ReturnsSuccessAndDeletesMaterial()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Delete(3); // Delete last material

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Materi 'Material 3' berhasil dihapus");

            // Verify remaining materials
            var remainingMaterials = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == 1)
                .ToListAsync();

            remainingMaterials.Should().HaveCount(2);
        }

        #endregion

        #region GetNextUrutan Tests

        [Fact]
        public async Task GetNextUrutan_WithExistingMaterials_ReturnsCorrectNextNumber()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetNextUrutan(1);

            // Assert
            result.Should().Be(4); // Next after existing 3 materials
        }

        [Fact]
        public async Task GetNextUrutan_WithNoMaterials_ReturnsOne()
        {
            // Arrange
            await SeedBasicTestData();

            // Act
            var result = await _service.GetNextUrutan(1);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task GetNextUrutan_WithNonExistentPelatihan_ReturnsOne()
        {
            // Arrange
            // No test data

            // Act
            var result = await _service.GetNextUrutan(999);

            // Assert
            result.Should().Be(1);
        }

        #endregion

        #region ReorderMaterials Tests

        [Fact]
        public async Task ReorderMaterials_WithValidPelatihanId_ReturnsSuccessAndReordersMaterials()
        {
            // Arrange
            await SeedTestDataWithGapsInOrder();

            // Act
            var result = await _service.ReorderMaterials(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Urutan materi berhasil diperbarui");

            // Verify materials are sequential
            var materials = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == 1)
                .OrderBy(m => m.Urutan)
                .ToListAsync();

            materials.Should().HaveCount(3);
            materials[0].Urutan.Should().Be(1);
            materials[1].Urutan.Should().Be(2);
            materials[2].Urutan.Should().Be(3);

            // Verify UpdatedAt is set
            materials.Should().AllSatisfy(m =>
                m.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public async Task ReorderMaterials_WithNoMaterials_ReturnsSuccess()
        {
            // Arrange
            await SeedBasicTestData();

            // Act
            var result = await _service.ReorderMaterials(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Urutan materi berhasil diperbarui");
        }

        #endregion

        #region MoveUp Tests

        [Fact]
        public async Task MoveUp_WithValidMaterial_ReturnsSuccessAndSwapsOrder()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveUp(2); // Move material 2 up

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Urutan materi berhasil diubah");

            // Verify order is swapped
            var materials = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == 1)
                .OrderBy(m => m.Urutan)
                .ToListAsync();

            var movedMaterial = materials.First(m => m.Id == 2);
            var swappedMaterial = materials.First(m => m.Id == 1);

            movedMaterial.Urutan.Should().Be(1); // Material 2 is now first
            swappedMaterial.Urutan.Should().Be(2); // Material 1 is now second
        }

        [Fact]
        public async Task MoveUp_WithFirstMaterial_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveUp(1); // Try to move first material up

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Materi sudah berada di urutan teratas");
        }

        [Fact]
        public async Task MoveUp_WithNonExistentMaterial_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveUp(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Materi tidak ditemukan");
        }

        #endregion

        #region MoveDown Tests

        [Fact]
        public async Task MoveDown_WithValidMaterial_ReturnsSuccessAndSwapsOrder()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveDown(1); // Move material 1 down

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Urutan materi berhasil diubah");

            // Verify order is swapped
            var materials = await _context.PelatihanMateri
                .Where(m => m.PelatihanId == 1)
                .OrderBy(m => m.Urutan)
                .ToListAsync();

            var movedMaterial = materials.First(m => m.Id == 1);
            var swappedMaterial = materials.First(m => m.Id == 2);

            movedMaterial.Urutan.Should().Be(2); // Material 1 is now second
            swappedMaterial.Urutan.Should().Be(1); // Material 2 is now first
        }

        [Fact]
        public async Task MoveDown_WithLastMaterial_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveDown(3); // Try to move last material down

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Materi sudah berada di urutan terbawah");
        }

        [Fact]
        public async Task MoveDown_WithNonExistentMaterial_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveDown(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Materi tidak ditemukan");
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task Add_WithDatabaseError_ReturnsFailureWithExceptionMessage()
        {
            // Arrange
            _context.Dispose(); // Force database error
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "New Material",
                TipeKonten = "text",
                Konten = "Content"
            };

            // Act
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
            result.Materi.Should().BeNull();
        }

        [Fact]
        public async Task Update_WithDatabaseError_ReturnsFailureWithExceptionMessage()
        {
            // Arrange
            await SeedTestData();
            _context.Dispose(); // Force database error after seeding

            var updatedMaterial = new PelatihanMateri
            {
                Id = 1,
                Judul = "Updated",
                TipeKonten = "text",
                Konten = "Content"
            };

            // Act
            var result = await _service.Update(updatedMaterial);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
            result.Materi.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WithDatabaseError_ReturnsFailureWithExceptionMessage()
        {
            // Arrange
            await SeedTestData();
            _context.Dispose(); // Force database error after seeding

            // Act
            var result = await _service.Delete(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
        }

        [Fact]
        public async Task ReorderMaterials_WithDatabaseError_ReturnsFailureWithExceptionMessage()
        {
            // Arrange
            await SeedTestData();
            _context.Dispose(); // Force database error after seeding

            // Act
            var result = await _service.ReorderMaterials(1);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan saat mengurutkan ulang materi:");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_AddUpdateMoveDelete_WorksCorrectly()
        {
            // Arrange
            await SeedBasicTestData();

            // Act & Assert - Add material
            var addResult = await _service.Add(new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "Material 1",
                TipeKonten = "text",
                Konten = "Content 1"
            });
            addResult.Success.Should().BeTrue();

            // Add second material
            var addResult2 = await _service.Add(new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = "Material 2",
                TipeKonten = "video",
                Konten = "Video URL"
            });
            addResult2.Success.Should().BeTrue();

            // Update first material
            var updateResult = await _service.Update(new PelatihanMateri
            {
                Id = addResult.Materi!.Id,
                Judul = "Updated Material 1",
                TipeKonten = "image",
                Konten = "Image URL"
            });
            updateResult.Success.Should().BeTrue();

            // Move second material up
            var moveResult = await _service.MoveUp(addResult2.Materi!.Id);
            moveResult.Success.Should().BeTrue();

            // Verify final state
            var finalMaterials = await _service.GetByPelatihanId(1);
            finalMaterials.Should().HaveCount(2);
            finalMaterials.First().Judul.Should().Be("Material 2");
            finalMaterials.Last().Judul.Should().Be("Updated Material 1");

            // Delete one material
            var deleteResult = await _service.Delete(addResult.Materi.Id);
            deleteResult.Success.Should().BeTrue();

            // Verify final count
            var remainingMaterials = await _service.GetByPelatihanId(1);
            remainingMaterials.Should().HaveCount(1);
        }

        #endregion

        #region Content Type Tests

        [Theory]
        [InlineData("text")]
        [InlineData("video")]
        [InlineData("image")]
        public async Task Add_WithDifferentContentTypes_WorksCorrectly(string contentType)
        {
            // Arrange
            await SeedBasicTestData();
            var newMaterial = new PelatihanMateri
            {
                PelatihanId = 1,
                Judul = $"Material for {contentType}",
                TipeKonten = contentType,
                Konten = $"Content for {contentType}"
            };

            // Act
            var result = await _service.Add(newMaterial);

            // Assert
            result.Success.Should().BeTrue();
            result.Materi!.TipeKonten.Should().Be(contentType);
        }

        #endregion

        #region Helper Methods

        private async Task SeedBasicTestData()
        {
            var pelatihan = new Pelatihan
            {
                Id = 1,
                Kode = "TR001",
                Judul = "Training 1",
                Deskripsi = "Test Training",
                DurasiMenit = 60,
                SkorMinimal = 70,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            _context.Pelatihan.Add(pelatihan);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestData()
        {
            await SeedBasicTestData();

            var materials = new List<PelatihanMateri>
            {
                new()
                {
                    Id = 1, PelatihanId = 1, Judul = "Material 1",
                    TipeKonten = "text", Konten = "Content 1",
                    Urutan = 1, CreatedAt = DateTime.Now
                },
                new()
                {
                    Id = 2, PelatihanId = 1, Judul = "Material 2",
                    TipeKonten = "video", Konten = "Video URL",
                    Urutan = 2, CreatedAt = DateTime.Now
                },
                new()
                {
                    Id = 3, PelatihanId = 1, Judul = "Material 3",
                    TipeKonten = "image", Konten = "Image URL",
                    Urutan = 3, CreatedAt = DateTime.Now
                }
            };

            _context.PelatihanMateri.AddRange(materials);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithDeletedPelatihan()
        {
            await SeedBasicTestData();

            var deletedPelatihan = new Pelatihan
            {
                Id = 2,
                Kode = "TR002",
                Judul = "Deleted Training",
                DurasiMenit = 60,
                SkorMinimal = 70,
                IsActive = true,
                IsDeleted = true, // Marked as deleted
                CreatedAt = DateTime.Now
            };

            _context.Pelatihan.Add(deletedPelatihan);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithGapsInOrder()
        {
            await SeedBasicTestData();

            var materials = new List<PelatihanMateri>
            {
                new()
                {
                    Id = 1, PelatihanId = 1, Judul = "Material 1",
                    TipeKonten = "text", Konten = "Content 1",
                    Urutan = 1, CreatedAt = DateTime.Now
                },
                new()
                {
                    Id = 2, PelatihanId = 1, Judul = "Material 2",
                    TipeKonten = "video", Konten = "Video URL",
                    Urutan = 5, CreatedAt = DateTime.Now // Gap in order
                },
                new()
                {
                    Id = 3, PelatihanId = 1, Judul = "Material 3",
                    TipeKonten = "image", Konten = "Image URL",
                    Urutan = 8, CreatedAt = DateTime.Now // Gap in order
                }
            };

            _context.PelatihanMateri.AddRange(materials);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}

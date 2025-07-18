// RamaExpress.Tests/Services/PelatihanSoalServiceTests.cs
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Tests.Services
{
    public class PelatihanSoalServiceTests : IDisposable
    {
        private readonly RamaExpressAppContext _context;
        private readonly PelatihanSoalService _service;

        public PelatihanSoalServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RamaExpressAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RamaExpressAppContext(options);

            // Create service
            _service = new PelatihanSoalService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetByPelatihanId Tests

        [Fact]
        public async Task GetByPelatihanId_WithValidPelatihanId_ReturnsQuestionsInOrder()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetByPelatihanId(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            var questions = result.ToList();
            questions[0].Urutan.Should().Be(1);
            questions[1].Urutan.Should().Be(2);
            questions[2].Urutan.Should().Be(3);
            questions[0].Pertanyaan.Should().Be("Question 1");
            questions[1].Pertanyaan.Should().Be("Question 2");
            questions[2].Pertanyaan.Should().Be("Question 3");
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
        public async Task GetById_WithValidId_ReturnsQuestionWithPelatihan()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetById(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Pertanyaan.Should().Be("Question 1");
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
        public async Task Add_WithValidQuestion_ReturnsSuccessAndAddsQuestion()
        {
            // Arrange
            await SeedTestData();
            var newQuestion = new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "New Question",
                OpsiA = "Option A",
                OpsiB = "Option B",
                OpsiC = "Option C",
                OpsiD = "Option D",
                JawabanBenar = "A"
            };

            // Act
            var result = await _service.Add(newQuestion);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Soal nomor 4 berhasil ditambahkan");
            result.Soal.Should().NotBeNull();
            result.Soal!.Urutan.Should().Be(4);
            result.Soal.JawabanBenar.Should().Be("A");
            result.Soal.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));

            // Verify in database
            var savedQuestion = await _context.PelatihanSoal.FindAsync(result.Soal.Id);
            savedQuestion.Should().NotBeNull();
            savedQuestion!.Pertanyaan.Should().Be("New Question");
        }

        [Fact]
        public async Task Add_WithNonExistentPelatihan_ReturnsFailure()
        {
            // Arrange
            var newQuestion = new PelatihanSoal
            {
                PelatihanId = 999,
                Pertanyaan = "New Question",
                OpsiA = "Option A",
                OpsiB = "Option B",
                OpsiC = "Option C",
                OpsiD = "Option D",
                JawabanBenar = "A"
            };

            // Act
            var result = await _service.Add(newQuestion);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pelatihan tidak ditemukan");
            result.Soal.Should().BeNull();
        }

        [Fact]
        public async Task Add_WithDeletedPelatihan_ReturnsFailure()
        {
            // Arrange
            await SeedTestDataWithDeletedPelatihan();
            var newQuestion = new PelatihanSoal
            {
                PelatihanId = 2,
                Pertanyaan = "New Question",
                OpsiA = "Option A",
                OpsiB = "Option B",
                OpsiC = "Option C",
                OpsiD = "Option D",
                JawabanBenar = "A"
            };

            // Act
            var result = await _service.Add(newQuestion);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Pelatihan tidak ditemukan");
            result.Soal.Should().BeNull();
        }

        [Theory]
        [InlineData("E")]
        [InlineData("X")]
        [InlineData("1")]
        [InlineData("")]
        public async Task Add_WithInvalidAnswer_ReturnsFailure(string invalidAnswer)
        {
            // Arrange
            await SeedTestData();
            var newQuestion = new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "New Question",
                OpsiA = "Option A",
                OpsiB = "Option B",
                OpsiC = "Option C",
                OpsiD = "Option D",
                JawabanBenar = invalidAnswer
            };

            // Act
            var result = await _service.Add(newQuestion);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Jawaban benar harus berupa A, B, C, atau D");
            result.Soal.Should().BeNull();
        }

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("c")]
        [InlineData("d")]
        public async Task Add_WithLowercaseAnswer_ConvertsToUppercaseAndSucceeds(string lowercaseAnswer)
        {
            // Arrange
            await SeedTestData();
            var newQuestion = new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "New Question",
                OpsiA = "Option A",
                OpsiB = "Option B",
                OpsiC = "Option C",
                OpsiD = "Option D",
                JawabanBenar = lowercaseAnswer
            };

            // Act
            var result = await _service.Add(newQuestion);

            // Assert
            result.Success.Should().BeTrue();
            result.Soal!.JawabanBenar.Should().Be(lowercaseAnswer.ToUpper());
        }

        [Fact]
        public async Task Add_TrimsWhitespaceFromAllFields()
        {
            // Arrange
            await SeedTestData();
            var newQuestion = new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "  New Question  ",
                OpsiA = "  Option A  ",
                OpsiB = "  Option B  ",
                OpsiC = "  Option C  ",
                OpsiD = "  Option D  ",
                JawabanBenar = "A"
            };

            // Act
            var result = await _service.Add(newQuestion);

            // Assert
            result.Success.Should().BeTrue();
            result.Soal!.Pertanyaan.Should().Be("New Question");
            result.Soal.OpsiA.Should().Be("Option A");
            result.Soal.OpsiB.Should().Be("Option B");
            result.Soal.OpsiC.Should().Be("Option C");
            result.Soal.OpsiD.Should().Be("Option D");
        }

        [Fact]
        public async Task Add_SetsCorrectOrderNumber()
        {
            // Arrange
            await SeedTestData();

            // Act
            var newQuestion = new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "Fourth Question",
                OpsiA = "A",
                OpsiB = "B",
                OpsiC = "C",
                OpsiD = "D",
                JawabanBenar = "A"
            };
            var result = await _service.Add(newQuestion);

            // Assert
            result.Success.Should().BeTrue();
            result.Soal!.Urutan.Should().Be(4); // Should be after existing 3 questions
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidQuestion_ReturnsSuccessAndUpdatesQuestion()
        {
            // Arrange
            await SeedTestData();
            var updatedQuestion = new PelatihanSoal
            {
                Id = 1,
                Pertanyaan = "Updated Question",
                OpsiA = "Updated A",
                OpsiB = "Updated B",
                OpsiC = "Updated C",
                OpsiD = "Updated D",
                JawabanBenar = "B"
            };

            // Act
            var result = await _service.Update(updatedQuestion);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Soal nomor 1 berhasil diperbarui");
            result.Soal.Should().NotBeNull();

            // Verify in database
            var savedQuestion = await _context.PelatihanSoal.FindAsync(1);
            savedQuestion!.Pertanyaan.Should().Be("Updated Question");
            savedQuestion.OpsiA.Should().Be("Updated A");
            savedQuestion.JawabanBenar.Should().Be("B");
        }

        [Fact]
        public async Task Update_WithNonExistentQuestion_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();
            var updatedQuestion = new PelatihanSoal
            {
                Id = 999,
                Pertanyaan = "Updated Question",
                OpsiA = "A",
                OpsiB = "B",
                OpsiC = "C",
                OpsiD = "D",
                JawabanBenar = "A"
            };

            // Act
            var result = await _service.Update(updatedQuestion);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Soal tidak ditemukan");
            result.Soal.Should().BeNull();
        }

        [Theory]
        [InlineData("E")]
        [InlineData("X")]
        public async Task Update_WithInvalidAnswer_ReturnsFailure(string invalidAnswer)
        {
            // Arrange
            await SeedTestData();
            var updatedQuestion = new PelatihanSoal
            {
                Id = 1,
                Pertanyaan = "Updated Question",
                OpsiA = "A",
                OpsiB = "B",
                OpsiC = "C",
                OpsiD = "D",
                JawabanBenar = invalidAnswer
            };

            // Act
            var result = await _service.Update(updatedQuestion);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Jawaban benar harus berupa A, B, C, atau D");
            result.Soal.Should().BeNull();
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidId_ReturnsSuccessAndDeletesQuestion()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Delete(2); // Delete middle question

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Soal nomor 2 berhasil dihapus");

            // Verify question is deleted
            var deletedQuestion = await _context.PelatihanSoal.FindAsync(2);
            deletedQuestion.Should().BeNull();

            // Verify remaining questions are reordered
            var remainingQuestions = await _context.PelatihanSoal
                .Where(q => q.PelatihanId == 1)
                .OrderBy(q => q.Urutan)
                .ToListAsync();

            remainingQuestions.Should().HaveCount(2);
            remainingQuestions[0].Urutan.Should().Be(1);
            remainingQuestions[1].Urutan.Should().Be(2);
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
            result.Message.Should().Be("Soal tidak ditemukan");
        }

        [Fact]
        public async Task Delete_LastQuestion_ReturnsSuccessAndDeletesQuestion()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.Delete(3); // Delete last question

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Soal nomor 3 berhasil dihapus");

            // Verify remaining questions
            var remainingQuestions = await _context.PelatihanSoal
                .Where(q => q.PelatihanId == 1)
                .ToListAsync();

            remainingQuestions.Should().HaveCount(2);
        }

        #endregion

        #region GetNextUrutan Tests

        [Fact]
        public async Task GetNextUrutan_WithExistingQuestions_ReturnsCorrectNextNumber()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetNextUrutan(1);

            // Assert
            result.Should().Be(4); // Next after existing 3 questions
        }

        [Fact]
        public async Task GetNextUrutan_WithNoQuestions_ReturnsOne()
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

        #region ReorderQuestions Tests

        [Fact]
        public async Task ReorderQuestions_WithValidPelatihanId_ReturnsSuccessAndReordersQuestions()
        {
            // Arrange
            await SeedTestDataWithGapsInOrder();

            // Act
            var result = await _service.ReorderQuestions(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Urutan soal berhasil diperbarui");

            // Verify questions are sequential
            var questions = await _context.PelatihanSoal
                .Where(q => q.PelatihanId == 1)
                .OrderBy(q => q.Urutan)
                .ToListAsync();

            questions.Should().HaveCount(3);
            questions[0].Urutan.Should().Be(1);
            questions[1].Urutan.Should().Be(2);
            questions[2].Urutan.Should().Be(3);
        }

        [Fact]
        public async Task ReorderQuestions_WithNoQuestions_ReturnsSuccess()
        {
            // Arrange
            await SeedBasicTestData();

            // Act
            var result = await _service.ReorderQuestions(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Urutan soal berhasil diperbarui");
        }

        #endregion

        #region MoveUp Tests

        [Fact]
        public async Task MoveUp_WithValidQuestion_ReturnsSuccessAndSwapsOrder()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveUp(2); // Move question 2 up

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Urutan soal berhasil diubah");

            // Verify order is swapped
            var questions = await _context.PelatihanSoal
                .Where(q => q.PelatihanId == 1)
                .OrderBy(q => q.Urutan)
                .ToListAsync();

            var movedQuestion = questions.First(q => q.Id == 2);
            var swappedQuestion = questions.First(q => q.Id == 1);

            movedQuestion.Urutan.Should().Be(1); // Question 2 is now first
            swappedQuestion.Urutan.Should().Be(2); // Question 1 is now second
        }

        [Fact]
        public async Task MoveUp_WithFirstQuestion_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveUp(1); // Try to move first question up

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Soal sudah berada di urutan teratas");
        }

        [Fact]
        public async Task MoveUp_WithNonExistentQuestion_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveUp(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Soal tidak ditemukan");
        }

        #endregion

        #region MoveDown Tests

        [Fact]
        public async Task MoveDown_WithValidQuestion_ReturnsSuccessAndSwapsOrder()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveDown(1); // Move question 1 down

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Urutan soal berhasil diubah");

            // Verify order is swapped
            var questions = await _context.PelatihanSoal
                .Where(q => q.PelatihanId == 1)
                .OrderBy(q => q.Urutan)
                .ToListAsync();

            var movedQuestion = questions.First(q => q.Id == 1);
            var swappedQuestion = questions.First(q => q.Id == 2);

            movedQuestion.Urutan.Should().Be(2); // Question 1 is now second
            swappedQuestion.Urutan.Should().Be(1); // Question 2 is now first
        }

        [Fact]
        public async Task MoveDown_WithLastQuestion_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveDown(3); // Try to move last question down

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Soal sudah berada di urutan terbawah");
        }

        [Fact]
        public async Task MoveDown_WithNonExistentQuestion_ReturnsFailure()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.MoveDown(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Soal tidak ditemukan");
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task Add_WithDatabaseError_ReturnsFailureWithExceptionMessage()
        {
            // Arrange
            _context.Dispose(); // Force database error
            var newQuestion = new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "New Question",
                OpsiA = "A",
                OpsiB = "B",
                OpsiC = "C",
                OpsiD = "D",
                JawabanBenar = "A"
            };

            // Act
            var result = await _service.Add(newQuestion);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
            result.Soal.Should().BeNull();
        }

        [Fact]
        public async Task Update_WithDatabaseError_ReturnsFailureWithExceptionMessage()
        {
            // Arrange
            await SeedTestData();
            _context.Dispose(); // Force database error after seeding

            var updatedQuestion = new PelatihanSoal
            {
                Id = 1,
                Pertanyaan = "Updated",
                OpsiA = "A",
                OpsiB = "B",
                OpsiC = "C",
                OpsiD = "D",
                JawabanBenar = "A"
            };

            // Act
            var result = await _service.Update(updatedQuestion);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().StartWith("Terjadi kesalahan:");
            result.Soal.Should().BeNull();
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

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_AddUpdateMoveDelete_WorksCorrectly()
        {
            // Arrange
            await SeedBasicTestData();

            // Act & Assert - Add question
            var addResult = await _service.Add(new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "Question 1",
                OpsiA = "A",
                OpsiB = "B",
                OpsiC = "C",
                OpsiD = "D",
                JawabanBenar = "A"
            });
            addResult.Success.Should().BeTrue();

            // Add second question
            var addResult2 = await _service.Add(new PelatihanSoal
            {
                PelatihanId = 1,
                Pertanyaan = "Question 2",
                OpsiA = "A",
                OpsiB = "B",
                OpsiC = "C",
                OpsiD = "D",
                JawabanBenar = "B"
            });
            addResult2.Success.Should().BeTrue();

            // Update first question
            var updateResult = await _service.Update(new PelatihanSoal
            {
                Id = addResult.Soal!.Id,
                Pertanyaan = "Updated Question 1",
                OpsiA = "A",
                OpsiB = "B",
                OpsiC = "C",
                OpsiD = "D",
                JawabanBenar = "C"
            });
            updateResult.Success.Should().BeTrue();

            // Move second question up
            var moveResult = await _service.MoveUp(addResult2.Soal!.Id);
            moveResult.Success.Should().BeTrue();

            // Verify final state
            var finalQuestions = await _service.GetByPelatihanId(1);
            finalQuestions.Should().HaveCount(2);
            finalQuestions.First().Pertanyaan.Should().Be("Question 2");
            finalQuestions.Last().Pertanyaan.Should().Be("Updated Question 1");

            // Delete one question
            var deleteResult = await _service.Delete(addResult.Soal.Id);
            deleteResult.Success.Should().BeTrue();

            // Verify final count
            var remainingQuestions = await _service.GetByPelatihanId(1);
            remainingQuestions.Should().HaveCount(1);
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

            var questions = new List<PelatihanSoal>
            {
                new()
                {
                    Id = 1, PelatihanId = 1, Pertanyaan = "Question 1",
                    OpsiA = "A1", OpsiB = "B1", OpsiC = "C1", OpsiD = "D1",
                    JawabanBenar = "A", Urutan = 1, CreatedAt = DateTime.Now
                },
                new()
                {
                    Id = 2, PelatihanId = 1, Pertanyaan = "Question 2",
                    OpsiA = "A2", OpsiB = "B2", OpsiC = "C2", OpsiD = "D2",
                    JawabanBenar = "B", Urutan = 2, CreatedAt = DateTime.Now
                },
                new()
                {
                    Id = 3, PelatihanId = 1, Pertanyaan = "Question 3",
                    OpsiA = "A3", OpsiB = "B3", OpsiC = "C3", OpsiD = "D3",
                    JawabanBenar = "C", Urutan = 3, CreatedAt = DateTime.Now
                }
            };

            _context.PelatihanSoal.AddRange(questions);
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

            var questions = new List<PelatihanSoal>
            {
                new()
                {
                    Id = 1, PelatihanId = 1, Pertanyaan = "Question 1",
                    OpsiA = "A1", OpsiB = "B1", OpsiC = "C1", OpsiD = "D1",
                    JawabanBenar = "A", Urutan = 1, CreatedAt = DateTime.Now
                },
                new()
                {
                    Id = 2, PelatihanId = 1, Pertanyaan = "Question 2",
                    OpsiA = "A2", OpsiB = "B2", OpsiC = "C2", OpsiD = "D2",
                    JawabanBenar = "B", Urutan = 5, CreatedAt = DateTime.Now // Gap in order
                },
                new()
                {
                    Id = 3, PelatihanId = 1, Pertanyaan = "Question 3",
                    OpsiA = "A3", OpsiB = "B3", OpsiC = "C3", OpsiD = "D3",
                    JawabanBenar = "C", Urutan = 8, CreatedAt = DateTime.Now // Gap in order
                }
            };

            _context.PelatihanSoal.AddRange(questions);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}


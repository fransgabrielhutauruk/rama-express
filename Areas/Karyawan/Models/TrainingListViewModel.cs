using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Karyawan.ViewModels
{
    // Training List ViewModels
    public class TrainingListViewModel
    {
        public IEnumerable<TrainingItemViewModel> Trainings { get; set; } = new List<TrainingItemViewModel>();
        public List<string> Categories { get; set; } = new List<string>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string Search { get; set; } = "";
        public string SelectedCategory { get; set; } = "";

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class TrainingItemViewModel
    {
        public int Id { get; set; }
        public string Judul { get; set; } = "";
        public string Deskripsi { get; set; } = "";
        public string Kategori { get; set; } = "";
        public int Durasi { get; set; }
        public int MaxPeserta { get; set; }
        public DateTime TanggalMulai { get; set; }
        public DateTime TanggalSelesai { get; set; }
        public bool IsEnrolled { get; set; }
        public int CurrentParticipants { get; set; }
    }

    // Training Detail ViewModels
    public class TrainingDetailViewModel
    {
        public int Id { get; set; }
        public string Judul { get; set; } = "";
        public string Deskripsi { get; set; } = "";
        public string Kategori { get; set; } = "";
        public int Durasi { get; set; }
        public int MaxPeserta { get; set; }
        public DateTime TanggalMulai { get; set; }
        public DateTime TanggalSelesai { get; set; }
        public string Syarat { get; set; } = "";
        public string Benefit { get; set; } = "";
        public bool IsEnrolled { get; set; }
        public int CurrentParticipants { get; set; }
        public List<TrainingMaterialViewModel> Materials { get; set; } = new List<TrainingMaterialViewModel>();
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public bool CanEnroll { get; set; }
    }

    public class TrainingMaterialViewModel
    {
        public int Id { get; set; }
        public string Judul { get; set; } = "";
        public string Deskripsi { get; set; } = "";
        public int Urutan { get; set; }
        public int Durasi { get; set; }
    }

    // My Training ViewModels
    public class MyTrainingListViewModel
    {
        public List<MyTrainingItemViewModel> Trainings { get; set; } = new List<MyTrainingItemViewModel>();
        public string SelectedStatus { get; set; } = "all";
    }

    public class MyTrainingItemViewModel
    {
        public int Id { get; set; }
        public string Judul { get; set; } = "";
        public string Kategori { get; set; } = "";
        public DateTime TanggalMulai { get; set; }
        public DateTime? TanggalSelesai { get; set; }
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public string Status { get; set; } = "";
    }

    // Training Progress ViewModels
    public class TrainingProgressViewModel
    {
        public int TrainingId { get; set; }
        public string TrainingTitle { get; set; } = "";
        public int OverallProgress { get; set; }
        public bool IsCompleted { get; set; }
        public List<TrainingMaterialProgressViewModel> Materials { get; set; } = new List<TrainingMaterialProgressViewModel>();
        public bool CanTakeExam { get; set; }
    }

    public class TrainingMaterialProgressViewModel
    {
        public int Id { get; set; }
        public string Judul { get; set; } = "";
        public string Deskripsi { get; set; } = "";
        public int Urutan { get; set; }
        public int Durasi { get; set; }
        public string TipeMateri { get; set; } = "";
        public string KontenUrl { get; set; } = "";
        public bool IsCompleted { get; set; }
    }

    // Exam ViewModels
    public class ExamViewModel
    {
        public int TrainingId { get; set; }
        public string TrainingTitle { get; set; } = "";
        public List<ExamQuestionViewModel> Questions { get; set; } = new List<ExamQuestionViewModel>();
        public int TimeLimit { get; set; } // in minutes
    }

    public class ExamQuestionViewModel
    {
        public int Id { get; set; }
        public string Pertanyaan { get; set; } = "";
        public string PilihanA { get; set; } = "";
        public string PilihanB { get; set; } = "";
        public string PilihanC { get; set; } = "";
        public string PilihanD { get; set; } = "";
        public int Urutan { get; set; }
    }

    public class ExamAnswerModel
    {
        public int QuestionId { get; set; }
        public string SelectedAnswer { get; set; } = "";
    }

    public class ExamResultViewModel
    {
        public int Score { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsPassed { get; set; }
        public int PassingScore { get; set; }
    }

    // Certificate ViewModels
    public class CertificateViewModel
    {
        public int Id { get; set; }
        public string NomorSertifikat { get; set; } = "";
        public string TrainingTitle { get; set; } = "";
        public DateTime TanggalTerbit { get; set; }
        public DateTime TanggalBerlaku { get; set; }
        public bool IsValid { get; set; }
    }

    // Additional Models for Missing Entities
    public class MateriProgress
    {
        public int Id { get; set; }
        public int MateriId { get; set; }
        public int UserId { get; set; }
        public DateTime TanggalSelesai { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class SoalUjian
    {
        public int Id { get; set; }
        public int PelatihanId { get; set; }
        public string Pertanyaan { get; set; } = "";
        public string PilihanA { get; set; } = "";
        public string PilihanB { get; set; } = "";
        public string PilihanC { get; set; } = "";
        public string PilihanD { get; set; } = "";
        public string JawabanBenar { get; set; } = "";
        public int Urutan { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Pelatihan Pelatihan { get; set; } = null!;
    }

    public class HasilUjian
    {
        public int Id { get; set; }
        public int PelatihanId { get; set; }
        public int UserId { get; set; }
        public DateTime TanggalUjian { get; set; }
        public int Skor { get; set; }
        public int JawabanBenar { get; set; }
        public int TotalSoal { get; set; }
        public bool IsLulus { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Pelatihan Pelatihan { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
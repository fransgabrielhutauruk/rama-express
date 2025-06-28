using RamaExpress.Models;
using RamaExpress.Areas.Karyawan.ViewModels;

namespace RamaExpress.Areas.Karyawan.Services
{
    public interface ITrainingService
    {
        Task<TrainingListViewModel> GetAvailableTrainingsAsync(int userId, int page = 1, int pageSize = 10, string search = "", string category = "");
        Task<TrainingDetailViewModel> GetTrainingDetailAsync(int trainingId, int userId);
        Task<bool> EnrollTrainingAsync(int trainingId, int userId);
        Task<MyTrainingListViewModel> GetMyTrainingsAsync(int userId, string status = "all");
        Task<TrainingProgressViewModel> GetTrainingProgressAsync(int trainingId, int userId);
        Task<bool> UpdateProgressAsync(int trainingId, int userId, int materialId);
        Task<bool> CompleteTrainingAsync(int trainingId, int userId);
        Task<ExamViewModel> GetExamAsync(int trainingId, int userId);
        Task<ExamResultViewModel> SubmitExamAsync(int trainingId, int userId, List<ExamAnswerModel> answers);
        Task<List<CertificateViewModel>> GetMyCertificatesAsync(int userId);
        Task<byte[]> DownloadCertificateAsync(int certificateId, int userId);
    }
}
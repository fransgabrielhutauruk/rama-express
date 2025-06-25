// Areas/Admin/Models/DashboardViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; } = 0;
        public int ActiveEmployees { get; set; } = 0;
        public int InactiveEmployees { get; set; } = 0;
        public int ActiveTrainings { get; set; } = 0;
        public int TotalTrainings { get; set; } = 0;
        public int TotalCertificates { get; set; } = 0;
        public int MonthlyIssuedCertificates { get; set; } = 0;
        public double CompletionRate { get; set; } = 0.0;
        public int CompletedTrainings { get; set; } = 0;
        public int TotalProgress { get; set; } = 0;

        // Calculated properties for display
        public string CompletionRateFormatted => $"{CompletionRate:F1}%";
        public double ActiveEmployeePercentage => TotalEmployees > 0 ? Math.Round((double)ActiveEmployees / TotalEmployees * 100, 1) : 0;
        public double TrainingCompletionRatio => TotalProgress > 0 ? Math.Round((double)CompletedTrainings / TotalProgress, 3) : 0;
    }

    public class RecentActivityViewModel
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Additional properties for better display
        public string StatusText => GetStatusText(Status);

        private string GetStatusText(string status)
        {
            return status?.ToLower() switch
            {
                "success" => "Selesai",
                "primary" => "Baru",
                "info" => "Sertifikat",
                "warning" => "Dibuat",
                "secondary" => "Pending",
                _ => "Unknown"
            };
        }
    }

    public class ChartDataViewModel
    {
        public List<MonthlyProgressData> MonthlyProgress { get; set; } = new List<MonthlyProgressData>();
        public List<PositionDistributionData> PositionDistribution { get; set; } = new List<PositionDistributionData>();

        // Summary properties
        public int TotalMonthlyCompletions => MonthlyProgress.Sum(x => x.CompletedTrainings);
        public int TotalMonthlyCertificates => MonthlyProgress.Sum(x => x.IssuedCertificates);
        public bool HasPositionData => PositionDistribution.Any();
        public bool HasProgressData => MonthlyProgress.Any();
    }

    public class MonthlyProgressData
    {
        public string Month { get; set; } = string.Empty;
        public int CompletedTrainings { get; set; } = 0;
        public int IssuedCertificates { get; set; } = 0;

        // Calculated properties
        public double CertificateToCompletionRatio => CompletedTrainings > 0 ?
            Math.Round((double)IssuedCertificates / CompletedTrainings * 100, 1) : 0;
    }

    public class PositionDistributionData
    {
        public string Position { get; set; } = string.Empty;
        public int Count { get; set; } = 0;

        // For percentage calculation (will be set when processing data)
        public double Percentage { get; set; } = 0;
    }

    // Additional ViewModel for API responses
    public class ApiResponseViewModel<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    // Dashboard statistics summary
    public class DashboardSummaryViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string SubText { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty; // "up", "down", "stable"
        public double TrendPercentage { get; set; } = 0;
    }
}
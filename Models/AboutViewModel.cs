namespace RamaExpress.Models
{
    public class AboutViewModel
    {
        public string CompanyName { get; set; } = "PT Rama Express";
        public string CompanyDescription { get; set; } = "";
        public string CompanyVision { get; set; } = "";
        public string CompanyMission { get; set; } = "";
        public List<ServiceModel> Services { get; set; } = new List<ServiceModel>();
        public List<CompanyValue> Values { get; set; } = new List<CompanyValue>();
    }

    public class CompanyValue
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconClass { get; set; } = "";
    }

    public class ServiceModel
    {
        public string ServiceName { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconClass { get; set; } = "";
    }

    public class ContactFormModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
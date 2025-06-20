namespace RamaExpress.Areas.Admin.Models
{
    public class PosisiWithCountViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int EmployeeCount { get; set; } // Jumlah karyawan dengan posisi ini

        // Constructor untuk mapping dari Posisi
        public PosisiWithCountViewModel() { }

        public PosisiWithCountViewModel(Posisi posisi, int employeeCount)
        {
            Id = posisi.Id;
            Name = posisi.Name;
            IsDeleted = posisi.IsDeleted;
            DeletedAt = posisi.DeletedAt;
            EmployeeCount = employeeCount;
        }
    }
}
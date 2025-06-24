// Areas/Admin/Models/PelatihanProgress.cs
namespace RamaExpress.Areas.Admin.Models
{
    public class PelatihanProgress
    {
        public int Id { get; set; }
        public int PelatihanId { get; set; }
        public int UserId { get; set; }
        public int MateriTerakhirId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Pelatihan Pelatihan { get; set; }
        public virtual User User { get; set; }
    }
}
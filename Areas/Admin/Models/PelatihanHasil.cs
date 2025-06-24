// Areas/Admin/Models/PelatihanHasil.cs
namespace RamaExpress.Areas.Admin.Models
{
    public class PelatihanHasil
    {
        public int Id { get; set; }
        public int PelatihanId { get; set; }
        public int UserId { get; set; }
        public int Skor { get; set; }
        public bool IsLulus { get; set; }
        public DateTime TanggalSelesai { get; set; }

        // Navigation properties
        public virtual Pelatihan Pelatihan { get; set; }
        public virtual User User { get; set; }
    }
}
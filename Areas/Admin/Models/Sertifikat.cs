// Areas/Admin/Models/Sertifikat.cs
namespace RamaExpress.Areas.Admin.Models
{
    public class Sertifikat
    {
        public int Id { get; set; }
        public int PelatihanId { get; set; }
        public int UserId { get; set; }
        public string NomorSertifikat { get; set; }
        public DateTime TanggalTerbit { get; set; }
        public DateTime TanggalKadaluarsa { get; set; }

        // Navigation properties
        public virtual Pelatihan Pelatihan { get; set; }
        public virtual User User { get; set; }
    }
}
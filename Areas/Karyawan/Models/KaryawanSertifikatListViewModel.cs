// Areas/Karyawan/Models/KaryawanSertifikatViewModel.cs
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Karyawan.Models
{
    public class KaryawanSertifikatListViewModel
    {
        public IEnumerable<Sertifikat> Sertifikats { get; set; } = new List<Sertifikat>();
        public int TotalSertifikat => Sertifikats.Count();
        public int SertifikatAktif => Sertifikats.Count(s => s.TanggalKadaluarsa == DateTime.MaxValue || s.TanggalKadaluarsa > DateTime.Now);
        public int SertifikatKadaluarsa => Sertifikats.Count(s => s.TanggalKadaluarsa != DateTime.MaxValue && s.TanggalKadaluarsa <= DateTime.Now);
    }

    public class KaryawanSertifikatDetailViewModel
    {
        public Sertifikat Sertifikat { get; set; } = new Sertifikat();
        public bool IsExpired { get; set; }
        public int DaysUntilExpiry { get; set; }

        public string StatusClass
        {
            get
            {
                if (IsExpired) return "danger";
                if (DaysUntilExpiry > 0 && DaysUntilExpiry <= 30) return "warning";
                return "success";
            }
        }

        public string StatusText
        {
            get
            {
                if (IsExpired) return "Kadaluarsa";
                if (Sertifikat.TanggalKadaluarsa == DateTime.MaxValue) return "Berlaku Selamanya";
                if (DaysUntilExpiry <= 30) return $"Akan kadaluarsa dalam {DaysUntilExpiry} hari";
                return "Aktif";
            }
        }
    }
}
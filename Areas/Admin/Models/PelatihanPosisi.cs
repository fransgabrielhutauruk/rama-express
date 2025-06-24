using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Models
{
    public class PelatihanPosisi
{
    public int PelatihanId { get; set; }
    public int PosisiId { get; set; }

    // Navigation properties
    public virtual Pelatihan Pelatihan { get; set; }
    public virtual Posisi Posisi { get; set; }
}
}
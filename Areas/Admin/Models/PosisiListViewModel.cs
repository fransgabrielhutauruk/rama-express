namespace RamaExpress.Areas.Admin.Models
{
    public class PosisiListViewModel
    {
        public IEnumerable<Posisi> Posisis { get; set; }
        public string SearchTerm { get; set; }
        public int TotalCount { get; set; }
    }
}
using Microsoft.AspNetCore.Mvc;

namespace RamaExpress.Areas.Karyawan.Controllers
{
    [Area("Karyawan")]
    public class HomeController : Controller
    {
        [Route("Karyawan")]
        public IActionResult Index()
        {
            return View();
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace RamaExpress.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("Tentang")]
        public IActionResult Tentang()
        {
            return View();
        }

        [Route("Kontak")]
        public IActionResult Kontak()
        {
            return View();
        }

        [Route("Layanan")]
        public IActionResult Layanan()
        {
            return View();
        }

        [Route("Training")]
        public IActionResult Training()
        {
            return View();
        }
    }
}

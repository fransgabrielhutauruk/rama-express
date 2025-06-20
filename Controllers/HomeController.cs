using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RamaExpress.Models;

namespace RamaExpress.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

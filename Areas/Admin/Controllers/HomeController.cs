using Microsoft.AspNetCore.Mvc;

namespace RamaExpress.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        [Route("Admin")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("Admin/Setting")]
        public IActionResult Setting()
        {
            return View();
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace RamaExpress.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}

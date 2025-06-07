using Microsoft.AspNetCore.Mvc;
using RamaExpress.Data;

namespace RamaExpress.Controllers
{
    public class AdminController : Controller
    {
        private readonly RamaExpressAppContext _context;
        public AdminController(RamaExpressAppContext context)
        {
            _context = context;
        }   
        public IActionResult Index()
        {
            var admin = _context.Admin.ToList();
            return View(admin);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}

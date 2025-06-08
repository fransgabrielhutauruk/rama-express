using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace RamaExpress.Controllers
{
    public class ToolsController : Controller
    {
        public IActionResult GenerateAdminPassword()
        {
            var password = "asdf1234";
            var hasher = new PasswordHasher<object>();
            var hashedPassword = hasher.HashPassword(null, password);

            return Content($"Hashed Password: {hashedPassword}");
        }
    }
}

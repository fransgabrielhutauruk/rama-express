using Microsoft.AspNetCore.Mvc;
using RamaExpress.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using RamaExpress.Data;
using Microsoft.AspNetCore.Identity;

namespace RamaExpress.Controllers
{
    public class UserController : Controller
    {
        private readonly RamaExpressAppContext _context;

        public UserController(RamaExpressAppContext context)
        {
            _context = context;
        }

        // GET: Display the login form
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /User/Login
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(null, user.Password, model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // Successful login
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Nama);

            return RedirectToAction("Index", "Dashboard");
        }

        //[HttpGet]
        //public IActionResult Logout()
        //{
        //    HttpContext.Session.Clear();
        //    return RedirectToAction("Login");
        //}

        //public async Task<IActionResult> Register(RegisterViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //        return View(model);

        //    var hasher = new PasswordHasher<User>();
        //    var user = new User
        //    {
        //        Email = model.Email,
        //        Nama = model.Nama,
        //        // store the hashed password
        //        Password = hasher.HashPassword(null, model.Password)
        //    };

        //    _context.User.Add(user);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction("Login");
        //}
    }
}

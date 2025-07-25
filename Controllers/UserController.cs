﻿using Microsoft.AspNetCore.Mvc;
using RamaExpress.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Controllers
{
    public class UserController : Controller
    {
        private readonly RamaExpressAppContext _context;

        public UserController(RamaExpressAppContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsDeleted == false    );

            if (user == null)
            {
                ModelState.AddModelError("", "Email or Password Salah");
                return View(model);
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(null, user.Password, model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email or Password Salah");
                return View(model);
            }

            if (user.IsDeleted)
            {
                ModelState.AddModelError("", "Akun tidak ditemukan atau telah dihapus");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Akun Anda sedang tidak aktif. Silakan hubungi administrator");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Nama);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("Posisi", user.Posisi);

            if (user.Role?.ToLower() == "admin")
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Pelatihan", new { area = "Karyawan" });
            }
        }

        [HttpGet]
        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

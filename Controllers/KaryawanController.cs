using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamaExpress.Data;
using RamaExpress.Data.Service;
using RamaExpress.Models;

namespace RamaExpress.Controllers
{
    public class KaryawanController : Controller
    {
        private readonly IKaryawanService _karyawanService;

        public KaryawanController(IKaryawanService karyawanService)
        {
            _karyawanService = karyawanService;
        }

        public async Task<IActionResult> Index()
        {
            var karyawan = await _karyawanService.GetAll();
            return View(karyawan);
        }
        public IActionResult Create()
        {
            return View();
        }

        //[HttpPost]
        //public async Task<IActionResult> Create(UserController user)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        await _karyawanService.Add(user);

        //        return RedirectToAction("Index");
        //    }
        //    return View(user);
        //}

    }
}

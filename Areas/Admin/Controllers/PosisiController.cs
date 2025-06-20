using Microsoft.AspNetCore.Mvc;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PosisiController : Controller
    {
        private readonly IPosisiService _posisiService;

        public PosisiController(IPosisiService posisiService)
        {
            _posisiService = posisiService;
        }

        [Route("Admin/Posisi")]
        public async Task<IActionResult> Index()
        {
            var posisiWithCount = await _posisiService.GetPosisiWithEmployeeCount();

            var viewModel = new PosisiListViewModel
            {
                Posisis = posisiWithCount.Select(p => new Posisi
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsDeleted = p.IsDeleted,
                    DeletedAt = p.DeletedAt
                }).ToList(),
                TotalCount = posisiWithCount.Count()
            };

            ViewBag.EmployeeCounts = posisiWithCount.ToDictionary(p => p.Id, p => p.EmployeeCount);

            return View(viewModel);
        }

        [Route("Admin/Posisi/Create")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Route("Admin/Posisi/Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Posisi model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _posisiService.Add(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }
        }

        [Route("Admin/Posisi/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var posisi = await _posisiService.GetById(id);
            if (posisi == null)
            {
                TempData["ErrorMessage"] = "Posisi tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            return View(posisi);
        }

        [Route("Admin/Posisi/Edit/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Posisi model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Data tidak valid";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _posisiService.Update(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }
        }

        [Route("Admin/Posisi/Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _posisiService.Delete(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [Route("Admin/Posisi/GetActive")]
        [HttpGet]
        public async Task<JsonResult> GetActive()
        {
            var posisi = await _posisiService.GetActivePosisi();
            return Json(posisi.Select(p => new { id = p.Id, name = p.Name }));
        }
    }
}
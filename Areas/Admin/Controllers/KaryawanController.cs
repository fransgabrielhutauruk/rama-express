using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class KaryawanController : Controller
    {
        private readonly IKaryawanService _karyawanService;
        private readonly IPosisiService _posisiService;

        public KaryawanController(IKaryawanService karyawanService, IPosisiService posisiService)
        {
            _karyawanService = karyawanService;
            _posisiService = posisiService;
        }

        [Route("Admin/Karyawan")]
        public async Task<IActionResult> Index(
            int page = 1,
            string searchTerm = null,
            string statusFilter = null,
            int pageSize = 10,
            string sortField = "Nama",
            string sortDirection = "asc")
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize));

            var (users, totalCount) = await _karyawanService.GetAllWithSearchAndSort(
                page, pageSize, searchTerm, statusFilter, sortField, sortDirection);

            var viewModel = new KaryawanListViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                PageSize = pageSize,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                SortField = sortField,
                SortDirection = sortDirection
            };

            return View(viewModel);
        }

        [Route("Admin/Karyawan/Create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadPosisiDropdown();
            return View();
        }

        [Route("Admin/Karyawan/Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model)
        {
            if (await _karyawanService.IsEmailExists(model.Email))
            {
                ModelState.AddModelError("Email", "Email sudah digunakan oleh pengguna lain");
            }

            if (!ModelState.IsValid)
            {
                await LoadPosisiDropdown();
                return View(model);
            }

            var result = await _karyawanService.AddKaryawan(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                await LoadPosisiDropdown();
                return RedirectToAction(nameof(Index));

            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                await LoadPosisiDropdown();
                return View(model);
            }
        }

        [Route("Admin/Karyawan/Details/{id}")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _karyawanService.GetById(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Karyawan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        [Route("Admin/Karyawan/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _karyawanService.GetById(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Karyawan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            await LoadPosisiDropdown();

            user.Password = string.Empty;
            return View(user);
        }

        [Route("Admin/Karyawan/Edit/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Data tidak valid";
                return RedirectToAction(nameof(Index));
            }

            if (await _karyawanService.IsEmailExists(model.Email, model.Id))
            {
                ModelState.AddModelError("Email", "Email sudah digunakan oleh pengguna lain");
            }

            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password minimal 6 karakter");
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.Remove("Password");
            }

            if (!ModelState.IsValid)
            {
                await LoadPosisiDropdown();
                return View(model);
            }

            var result = await _karyawanService.UpdateKaryawan(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                await LoadPosisiDropdown();
                return RedirectToAction(nameof(Index));

            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                await LoadPosisiDropdown();
                return View(model);
            }
        }

        [Route("Admin/Karyawan/Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _karyawanService.DeleteKaryawan(id);

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

        [Route("Admin/Karyawan/ToggleStatus")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _karyawanService.ToggleActiveStatus(id);

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

        private async Task LoadPosisiDropdown()
        {
            var posisiList = await _posisiService.GetActivePosisi();
            ViewBag.PosisiList = new SelectList(posisiList, "Name", "Name", null);
        }
    }
}
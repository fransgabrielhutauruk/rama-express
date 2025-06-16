// Areas/Admin/Controllers/KaryawanController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class KaryawanController : Controller
    {
        private readonly IKaryawanService _karyawanService;

        public KaryawanController(IKaryawanService karyawanService)
        {
            _karyawanService = karyawanService;
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
        public IActionResult Create()
        {
            return View();
        }
    }
}
// Areas/Admin/Controllers/PelatihanController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RamaExpress.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PelatihanController : Controller
    {
        private readonly IPelatihanService _pelatihanService;
        private readonly IPelatihanMateriService _materiService;
        private readonly IPelatihanSoalService _soalService;
        private readonly IPelatihanSertifikatService _sertifikatService;
        private readonly RamaExpressAppContext _context;

        public PelatihanController(
            IPelatihanService pelatihanService,
            IPelatihanMateriService materiService,
            IPelatihanSoalService soalService,
            IPelatihanSertifikatService sertifikatService,
            RamaExpressAppContext context)
        {
            _pelatihanService = pelatihanService;
            _materiService = materiService;
            _soalService = soalService;
            _sertifikatService = sertifikatService;
            _context = context;
        }

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

            var (pelatihans, totalCount) = await _pelatihanService.GetAllWithSearch(page, pageSize, searchTerm, statusFilter);

            var viewModel = new PelatihanListViewModel
            {
                Pelatihans = pelatihans,
                CurrentPage = page,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                PageSize = pageSize,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter
            };

            return View(viewModel);
        }

        [Route("Admin/Pelatihan/Create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Positions = await GetPositionsSelectList();
            return View();
        }

        [Route("Admin/Pelatihan/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(Pelatihan model, List<int> SelectedPositions)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Positions = await GetPositionsSelectList();
                return View(model);
            }

            var result = await _pelatihanService.Add(model);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                ViewBag.Positions = await GetPositionsSelectList();
                return View(model);
            }

            // Add position assignments
            if (SelectedPositions != null && SelectedPositions.Any())
            {
                foreach (var posisiId in SelectedPositions)
                {
                    var assignment = new PelatihanPosisi
                    {
                        PelatihanId = result.Pelatihan.Id,
                        PosisiId = posisiId
                    };
                    _context.PelatihanPosisi.Add(assignment);
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [Route("Admin/Pelatihan/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var pelatihan = await _pelatihanService.GetById(id);
            if (pelatihan == null)
            {
                TempData["ErrorMessage"] = "Pelatihan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Positions = await GetPositionsSelectList();
            ViewBag.SelectedPositions = await _context.PelatihanPosisi
                .Where(pp => pp.PelatihanId == id)
                .Select(pp => pp.PosisiId)
                .ToListAsync();

            return View(pelatihan);
        }

        [Route("Admin/Pelatihan/Edit/{id}")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Pelatihan model, List<int> SelectedPositions)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Data tidak valid";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Positions = await GetPositionsSelectList();
                ViewBag.SelectedPositions = SelectedPositions;
                return View(model);
            }

            var result = await _pelatihanService.Update(model);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                ViewBag.Positions = await GetPositionsSelectList();
                ViewBag.SelectedPositions = SelectedPositions;
                return View(model);
            }

            // Update position assignments
            var existingAssignments = await _context.PelatihanPosisi
                .Where(pp => pp.PelatihanId == id)
                .ToListAsync();

            _context.PelatihanPosisi.RemoveRange(existingAssignments);

            if (SelectedPositions != null && SelectedPositions.Any())
            {
                foreach (var posisiId in SelectedPositions)
                {
                    var assignment = new PelatihanPosisi
                    {
                        PelatihanId = model.Id,
                        PosisiId = posisiId
                    };
                    _context.PelatihanPosisi.Add(assignment);
                }
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [Route("Admin/Pelatihan/Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            Console.WriteLine($"Delete called with ID: {id}");

            var result = await _pelatihanService.Delete(id);

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

        // MATERIAL MANAGEMENT
        [Route("Admin/Pelatihan/Materials/{pelatihanId}")]
        public async Task<IActionResult> Materials(int pelatihanId)
        {
            var pelatihan = await _pelatihanService.GetById(pelatihanId);
            if (pelatihan == null)
            {
                TempData["ErrorMessage"] = "Pelatihan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var materials = await _materiService.GetByPelatihanId(pelatihanId);

            ViewBag.Pelatihan = pelatihan;
            return View(materials);
        }

        [Route("Admin/Pelatihan/Materials/Create/{pelatihanId}")]
        [HttpGet]
        public async Task<IActionResult> CreateMaterial(int pelatihanId)
        {
            var pelatihan = await _pelatihanService.GetById(pelatihanId);
            if (pelatihan == null)
            {
                TempData["ErrorMessage"] = "Pelatihan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var model = new PelatihanMateri
            {
                PelatihanId = pelatihanId
            };

            ViewBag.Pelatihan = pelatihan;
            return View(model);
        }

        [Route("Admin/Pelatihan/Materials/Create/{pelatihanId}")]
        [HttpPost]
        public async Task<IActionResult> CreateMaterial(PelatihanMateri model)
        {
            if (!ModelState.IsValid)
            {
                var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
                ViewBag.Pelatihan = pelatihan;
                return View(model);
            }

            var result = await _materiService.Add(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Materials), new { pelatihanId = model.PelatihanId });
        }

        [Route("Admin/Pelatihan/Materials/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> EditMaterial(int id)
        {
            var material = await _materiService.GetById(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "Materi tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Pelatihan = material.Pelatihan;
            return View(material);
        }

        [Route("Admin/Pelatihan/Materials/Edit/{id}")]
        [HttpPost]
        public async Task<IActionResult> EditMaterial(int id, PelatihanMateri model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Data tidak valid";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
                ViewBag.Pelatihan = pelatihan;
                return View(model);
            }

            var result = await _materiService.Update(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Materials), new { pelatihanId = model.PelatihanId });
        }

        [Route("Admin/Pelatihan/Materials/Delete/{id}")]
        [HttpPost]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            var material = await _materiService.GetById(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "Materi tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var pelatihanId = material.PelatihanId;
            var result = await _materiService.Delete(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Materials), new { pelatihanId = pelatihanId });
        }

        // QUESTION MANAGEMENT
        [Route("Admin/Pelatihan/Exam/{pelatihanId}")]
        public async Task<IActionResult> ExamQuestions(int pelatihanId)
        {
            var pelatihan = await _pelatihanService.GetById(pelatihanId);
            if (pelatihan == null)
            {
                TempData["ErrorMessage"] = "Pelatihan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var questions = await _soalService.GetByPelatihanId(pelatihanId);

            ViewBag.Pelatihan = pelatihan;
            return View(questions);
        }

        [Route("Admin/Pelatihan/Exam/Create/{pelatihanId}")]
        [HttpGet]
        public async Task<IActionResult> CreateQuestion(int pelatihanId)
        {
            var pelatihan = await _pelatihanService.GetById(pelatihanId);
            if (pelatihan == null)
            {
                TempData["ErrorMessage"] = "Pelatihan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var model = new PelatihanSoal
            {
                PelatihanId = pelatihanId
            };

            ViewBag.Pelatihan = pelatihan;
            return View(model);
        }

        [Route("Admin/Pelatihan/Exam/Create/{pelatihanId}")]
        [HttpPost]
        public async Task<IActionResult> CreateQuestion(PelatihanSoal model)
        {
            if (!ModelState.IsValid)
            {
                var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
                ViewBag.Pelatihan = pelatihan;
                return View(model);
            }

            var result = await _soalService.Add(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(ExamQuestions), new { pelatihanId = model.PelatihanId });
        }

        [Route("Admin/Pelatihan/Exam/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> EditQuestion(int id)
        {
            var question = await _soalService.GetById(id);
            if (question == null)
            {
                TempData["ErrorMessage"] = "Soal tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Pelatihan = question.Pelatihan;
            return View(question);
        }

        [Route("Admin/Pelatihan/Exam/Edit/{id}")]
        [HttpPost]
        public async Task<IActionResult> EditQuestion(int id, PelatihanSoal model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Data tidak valid";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
                ViewBag.Pelatihan = pelatihan;
                return View(model);
            }

            var result = await _soalService.Update(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(ExamQuestions), new { pelatihanId = model.PelatihanId });
        }

        [Route("Admin/Pelatihan/Exam/Delete/{id}")]
        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _soalService.GetById(id);
            if (question == null)
            {
                TempData["ErrorMessage"] = "Soal tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var pelatihanId = question.PelatihanId;
            var result = await _soalService.Delete(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(ExamQuestions), new { pelatihanId = pelatihanId });
        }

        // AJAX MOVE METHODS
        [Route("Admin/Pelatihan/Materials/MoveUp/{id}")]
        [HttpPost]
        public async Task<IActionResult> MoveMaterialUp(int id)
        {
            var result = await _materiService.MoveUp(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("Admin/Pelatihan/Materials/MoveDown/{id}")]
        [HttpPost]
        public async Task<IActionResult> MoveMaterialDown(int id)
        {
            var result = await _materiService.MoveDown(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("Admin/Pelatihan/Exam/MoveUp/{id}")]
        [HttpPost]
        public async Task<IActionResult> MoveQuestionUp(int id)
        {
            var result = await _soalService.MoveUp(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("Admin/Pelatihan/Exam/MoveDown/{id}")]
        [HttpPost]
        public async Task<IActionResult> MoveQuestionDown(int id)
        {
            var result = await _soalService.MoveDown(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        private async Task<SelectList> GetPositionsSelectList()
        {
            try
            {
                var positions = await _context.Posisi
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name })
                    .ToListAsync();

                if (positions == null || !positions.Any())
                {
                    return new SelectList(new List<SelectListItem>(), "Value", "Text");
                }

                return new SelectList(positions, "Id", "Name");
            }
            catch (Exception)
            {
                return new SelectList(new List<SelectListItem>(), "Value", "Text");
            }
        }

        // CERTIFICATE MANAGEMENT
        [Route("Admin/Pelatihan/Certificate/{pelatihanId}")]
        public async Task<IActionResult> Certificate(int pelatihanId)
        {
            var pelatihan = await _pelatihanService.GetById(pelatihanId);
            if (pelatihan == null)
            {
                TempData["ErrorMessage"] = "Pelatihan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var certificate = await _sertifikatService.GetByPelatihanId(pelatihanId);

            ViewBag.Pelatihan = pelatihan;
            ViewBag.HasCertificate = certificate != null;

            return View(certificate);
        }

        [Route("Admin/Pelatihan/Certificate/Create/{pelatihanId}")]
        [HttpGet]
        public async Task<IActionResult> CreateCertificate(int pelatihanId)
        {
            var pelatihan = await _pelatihanService.GetById(pelatihanId);
            if (pelatihan == null)
            {
                TempData["ErrorMessage"] = "Pelatihan tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            // Check if certificate already exists
            var existingCertificate = await _sertifikatService.GetByPelatihanId(pelatihanId);
            if (existingCertificate != null)
            {
                TempData["ErrorMessage"] = "Pengaturan sertifikat untuk pelatihan ini sudah ada";
                return RedirectToAction(nameof(Certificate), new { pelatihanId = pelatihanId });
            }

            var model = new PelatihanSertifikat
            {
                PelatihanId = pelatihanId,
                TemplateName = $"Sertifikat {pelatihan.Judul}",
                ExpirationType = "never",
                IsSertifikatActive = true
            };

            ViewBag.Pelatihan = pelatihan;
            return View(model);
        }

        [Route("Admin/Pelatihan/Certificate/Create/{pelatihanId}")]
        [HttpPost]
        public async Task<IActionResult> CreateCertificate(PelatihanSertifikat model)
        {
            if (!ModelState.IsValid)
            {
                var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
                ViewBag.Pelatihan = pelatihan;
                return View(model);
            }

            var result = await _sertifikatService.Add(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Certificate), new { pelatihanId = model.PelatihanId });
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
                ViewBag.Pelatihan = pelatihan;
                return View(model);
            }
        }

        [Route("Admin/Pelatihan/Certificate/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> EditCertificate(int id)
        {
            var certificate = await _sertifikatService.GetById(id);
            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Pengaturan sertifikat tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Pelatihan = certificate.Pelatihan;
            return View(certificate);
        }

        [Route("Admin/Pelatihan/Certificate/Edit/{id}")]
        [HttpPost]
        public async Task<IActionResult> EditCertificate(int id, PelatihanSertifikat model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Data tidak valid";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
                ViewBag.Pelatihan = pelatihan;
                return View(model);
            }

            var result = await _sertifikatService.Update(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Certificate), new { pelatihanId = model.PelatihanId });
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
                ViewBag.Pelatihan = pelatihan;
                return View(model);
            }
        }

        [Route("Admin/Pelatihan/Certificate/Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            var certificate = await _sertifikatService.GetById(id);
            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Pengaturan sertifikat tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var pelatihanId = certificate.PelatihanId;
            var result = await _sertifikatService.Delete(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Certificate), new { pelatihanId = pelatihanId });
        }
    }
}
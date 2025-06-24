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
        private readonly RamaExpressAppContext _context;

        public PelatihanController(
            IPelatihanService pelatihanService,
            IPelatihanMateriService materiService,
            IPelatihanSoalService soalService,
            RamaExpressAppContext context)
        {
            _pelatihanService = pelatihanService;
            _materiService = materiService;
            _soalService = soalService;
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
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;

                // Add pelatihan
                await _pelatihanService.Add(model);

                // Add position assignments
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
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Pelatihan berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Positions = await GetPositionsSelectList();
            return View(model);
        }

        [Route("Admin/Pelatihan/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var pelatihan = await _pelatihanService.GetById(id);
            if (pelatihan == null)
            {
                return NotFound();
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
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                model.UpdatedAt = DateTime.Now;
                await _pelatihanService.Update(model);

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

                TempData["Success"] = "Pelatihan berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Positions = await GetPositionsSelectList();
            ViewBag.SelectedPositions = SelectedPositions;
            return View(model);
        }

        [Route("Admin/Pelatihan/Delete/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _pelatihanService.Delete(id);
            TempData["Success"] = "Pelatihan berhasil dihapus!";
            return RedirectToAction(nameof(Index));
        }

        // MATERIAL MANAGEMENT
        [Route("Admin/Pelatihan/Materials/{pelatihanId}")]
        public async Task<IActionResult> Materials(int pelatihanId)
        {
            var pelatihan = await _pelatihanService.GetById(pelatihanId);
            if (pelatihan == null)
            {
                return NotFound();
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
                return NotFound();
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
            if (ModelState.IsValid)
            {
                await _materiService.Add(model);
                TempData["Success"] = "Materi berhasil ditambahkan!";
                return RedirectToAction(nameof(Materials), new { pelatihanId = model.PelatihanId });
            }

            var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
            ViewBag.Pelatihan = pelatihan;
            return View(model);
        }

        [Route("Admin/Pelatihan/Materials/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> EditMaterial(int id)
        {
            var material = await _materiService.GetById(id);
            if (material == null)
            {
                return NotFound();
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
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _materiService.Update(model);
                TempData["Success"] = "Materi berhasil diperbarui!";
                return RedirectToAction(nameof(Materials), new { pelatihanId = model.PelatihanId });
            }

            var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
            ViewBag.Pelatihan = pelatihan;
            return View(model);
        }

        [Route("Admin/Pelatihan/Materials/Delete/{id}")]
        [HttpPost]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            var material = await _materiService.GetById(id);
            if (material == null)
            {
                return NotFound();
            }

            var pelatihanId = material.PelatihanId;
            await _materiService.Delete(id);
            TempData["Success"] = "Materi berhasil dihapus!";
            return RedirectToAction(nameof(Materials), new { pelatihanId = pelatihanId });
        }

        // QUESTION MANAGEMENT
        [Route("Admin/Pelatihan/Exam/{pelatihanId}")]
        public async Task<IActionResult> ExamQuestions(int pelatihanId)
        {
            var pelatihan = await _pelatihanService.GetById(pelatihanId);
            if (pelatihan == null)
            {
                return NotFound();
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
                return NotFound();
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
            if (ModelState.IsValid)
            {
                await _soalService.Add(model);
                TempData["Success"] = "Soal berhasil ditambahkan!";
                return RedirectToAction(nameof(ExamQuestions), new { pelatihanId = model.PelatihanId });
            }

            var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
            ViewBag.Pelatihan = pelatihan;
            return View(model);
        }

        [Route("Admin/Pelatihan/Exam/Edit/{id}")]
        [HttpGet]
        public async Task<IActionResult> EditQuestion(int id)
        {
            var question = await _soalService.GetById(id);
            if (question == null)
            {
                return NotFound();
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
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _soalService.Update(model);
                TempData["Success"] = "Soal berhasil diperbarui!";
                return RedirectToAction(nameof(ExamQuestions), new { pelatihanId = model.PelatihanId });
            }

            var pelatihan = await _pelatihanService.GetById(model.PelatihanId);
            ViewBag.Pelatihan = pelatihan;
            return View(model);
        }

        [Route("Admin/Pelatihan/Exam/Delete/{id}")]
        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _soalService.GetById(id);
            if (question == null)
            {
                return NotFound();
            }

            var pelatihanId = question.PelatihanId;
            await _soalService.Delete(id);
            TempData["Success"] = "Soal berhasil dihapus!";
            return RedirectToAction(nameof(ExamQuestions), new { pelatihanId = pelatihanId });
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

        [Route("Admin/Pelatihan/Materials/MoveUp/{id}")]
        [HttpPost]
        public async Task<IActionResult> MoveMaterialUp(int id)
        {
            try
            {
                var material = await _materiService.GetById(id);
                if (material == null)
                {
                    return Json(new { success = false, message = "Materi tidak ditemukan" });
                }

                // Find the material above this one
                var materials = await _materiService.GetByPelatihanId(material.PelatihanId);
                var currentMaterial = materials.FirstOrDefault(m => m.Id == id);
                var previousMaterial = materials
                    .Where(m => m.Urutan < currentMaterial.Urutan)
                    .OrderByDescending(m => m.Urutan)
                    .FirstOrDefault();

                if (previousMaterial == null)
                {
                    return Json(new { success = false, message = "Materi sudah berada di urutan teratas" });
                }

                // Swap the order
                var tempOrder = currentMaterial.Urutan;
                currentMaterial.Urutan = previousMaterial.Urutan;
                previousMaterial.Urutan = tempOrder;

                // Update both materials
                await _materiService.Update(currentMaterial);
                await _materiService.Update(previousMaterial);

                return Json(new { success = true, message = "Urutan materi berhasil diubah" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        [Route("Admin/Pelatihan/Materials/MoveDown/{id}")]
        [HttpPost]
        public async Task<IActionResult> MoveMaterialDown(int id)
        {
            try
            {
                var material = await _materiService.GetById(id);
                if (material == null)
                {
                    return Json(new { success = false, message = "Materi tidak ditemukan" });
                }

                // Find the material below this one
                var materials = await _materiService.GetByPelatihanId(material.PelatihanId);
                var currentMaterial = materials.FirstOrDefault(m => m.Id == id);
                var nextMaterial = materials
                    .Where(m => m.Urutan > currentMaterial.Urutan)
                    .OrderBy(m => m.Urutan)
                    .FirstOrDefault();

                if (nextMaterial == null)
                {
                    return Json(new { success = false, message = "Materi sudah berada di urutan terbawah" });
                }

                // Swap the order
                var tempOrder = currentMaterial.Urutan;
                currentMaterial.Urutan = nextMaterial.Urutan;
                nextMaterial.Urutan = tempOrder;

                // Update both materials
                await _materiService.Update(currentMaterial);
                await _materiService.Update(nextMaterial);

                return Json(new { success = true, message = "Urutan materi berhasil diubah" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        [Route("Admin/Pelatihan/Exam/MoveUp/{id}")]
        [HttpPost]
        public async Task<IActionResult> MoveQuestionUp(int id)
        {
            try
            {
                var question = await _soalService.GetById(id);
                if (question == null)
                {
                    return Json(new { success = false, message = "Soal tidak ditemukan" });
                }

                // Find the question above this one
                var questions = await _soalService.GetByPelatihanId(question.PelatihanId);
                var currentQuestion = questions.FirstOrDefault(q => q.Id == id);
                var previousQuestion = questions
                    .Where(q => q.Urutan < currentQuestion.Urutan)
                    .OrderByDescending(q => q.Urutan)
                    .FirstOrDefault();

                if (previousQuestion == null)
                {
                    return Json(new { success = false, message = "Soal sudah berada di urutan teratas" });
                }

                // Swap the order
                var tempOrder = currentQuestion.Urutan;
                currentQuestion.Urutan = previousQuestion.Urutan;
                previousQuestion.Urutan = tempOrder;

                // Update both questions
                await _soalService.Update(currentQuestion);
                await _soalService.Update(previousQuestion);

                return Json(new { success = true, message = "Urutan soal berhasil diubah" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        [Route("Admin/Pelatihan/Exam/MoveDown/{id}")]
        [HttpPost]
        public async Task<IActionResult> MoveQuestionDown(int id)
        {
            try
            {
                var question = await _soalService.GetById(id);
                if (question == null)
                {
                    return Json(new { success = false, message = "Soal tidak ditemukan" });
                }

                // Find the question below this one
                var questions = await _soalService.GetByPelatihanId(question.PelatihanId);
                var currentQuestion = questions.FirstOrDefault(q => q.Id == id);
                var nextQuestion = questions
                    .Where(q => q.Urutan > currentQuestion.Urutan)
                    .OrderBy(q => q.Urutan)
                    .FirstOrDefault();

                if (nextQuestion == null)
                {
                    return Json(new { success = false, message = "Soal sudah berada di urutan terbawah" });
                }

                // Swap the order
                var tempOrder = currentQuestion.Urutan;
                currentQuestion.Urutan = nextQuestion.Urutan;
                nextQuestion.Urutan = tempOrder;

                // Update both questions
                await _soalService.Update(currentQuestion);
                await _soalService.Update(nextQuestion);

                return Json(new { success = true, message = "Urutan soal berhasil diubah" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }
    }
}
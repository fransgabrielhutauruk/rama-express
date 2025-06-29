using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;
using RamaExpress.Areas.Karyawan.Models;
using System.Text;

namespace RamaExpress.Areas.Karyawan.Controllers
{
    [Area("Karyawan")]
    public class SertifikatController : Controller
    {
        private readonly RamaExpressAppContext _context;
        private readonly ILogger<SertifikatController> _logger;

        public SertifikatController(RamaExpressAppContext context, ILogger<SertifikatController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Route("Karyawan/Sertifikat")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var certificates = await _context.Sertifikat
                    .Include(s => s.Pelatihan)
                    .Include(s => s.User)
                    .Where(s => s.UserId == userId.Value)
                    .OrderByDescending(s => s.TanggalTerbit)
                    .ToListAsync();

                var viewModel = new SertifikatIndexViewModel
                {
                    Certificates = certificates,
                    TotalCertificates = certificates.Count,
                    ValidCertificates = certificates.Count(c => c.TanggalKadaluarsa > DateTime.Now || c.TanggalKadaluarsa == DateTime.MaxValue),
                    ExpiredCertificates = certificates.Count(c => c.TanggalKadaluarsa <= DateTime.Now && c.TanggalKadaluarsa != DateTime.MaxValue)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates for user {UserId}", HttpContext.Session.GetInt32("UserId"));
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data sertifikat.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Route("Karyawan/Sertifikat/Detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var certificate = await _context.Sertifikat
                    .Include(s => s.Pelatihan)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId.Value);

                if (certificate == null)
                {
                    TempData["ErrorMessage"] = "Sertifikat tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                // Get training result
                var hasil = await _context.PelatihanHasil
                    .FirstOrDefaultAsync(h => h.UserId == userId.Value && h.PelatihanId == certificate.PelatihanId);

                var viewModel = new SertifikatDetailViewModel
                {
                    Certificate = certificate,
                    TrainingResult = hasil,
                    IsValid = certificate.TanggalKadaluarsa > DateTime.Now || certificate.TanggalKadaluarsa == DateTime.MaxValue,
                    DaysUntilExpiry = certificate.TanggalKadaluarsa == DateTime.MaxValue ?
                        null : (int)(certificate.TanggalKadaluarsa - DateTime.Now).TotalDays
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate detail {CertificateId} for user {UserId}", id, HttpContext.Session.GetInt32("UserId"));
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat detail sertifikat.";
                return RedirectToAction("Index");
            }
        }

        [Route("Karyawan/Sertifikat/Download/{id}")]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var certificate = await _context.Sertifikat
                    .Include(s => s.Pelatihan)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId.Value);

                if (certificate == null)
                {
                    TempData["ErrorMessage"] = "Sertifikat tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                // Check if certificate is still valid
                if (certificate.TanggalKadaluarsa <= DateTime.Now && certificate.TanggalKadaluarsa != DateTime.MaxValue)
                {
                    TempData["WarningMessage"] = "Sertifikat ini sudah tidak berlaku.";
                }

                // Generate PDF (placeholder implementation)
                var pdfBytes = await GenerateCertificatePdf(certificate);

                var fileName = $"Sertifikat_{certificate.NomorSertifikat.Replace("/", "_")}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate {CertificateId} for user {UserId}", id, HttpContext.Session.GetInt32("UserId"));
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengunduh sertifikat.";
                return RedirectToAction("Detail", new { id = id });
            }
        }

        [Route("Karyawan/Sertifikat/Verify/{nomorSertifikat}")]
        public async Task<IActionResult> Verify(string nomorSertifikat)
        {
            try
            {
                var certificate = await _context.Sertifikat
                    .Include(s => s.Pelatihan)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.NomorSertifikat == nomorSertifikat);

                var viewModel = new SertifikatVerificationViewModel
                {
                    NomorSertifikat = nomorSertifikat,
                    Certificate = certificate,
                    IsValid = certificate != null &&
                              (certificate.TanggalKadaluarsa > DateTime.Now || certificate.TanggalKadaluarsa == DateTime.MaxValue)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying certificate {CertificateNumber}", nomorSertifikat);
                var viewModel = new SertifikatVerificationViewModel
                {
                    NomorSertifikat = nomorSertifikat,
                    Certificate = null,
                    IsValid = false
                };
                return View(viewModel);
            }
        }

        [Route("Karyawan/Sertifikat/Share/{id}")]
        public async Task<IActionResult> Share(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "User", new { area = "" });
                }

                var certificate = await _context.Sertifikat
                    .Include(s => s.Pelatihan)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId.Value);

                if (certificate == null)
                {
                    return Json(new { success = false, message = "Sertifikat tidak ditemukan" });
                }

                var shareUrl = Url.Action("Verify", "Sertifikat", new { area = "Karyawan", nomorSertifikat = certificate.NomorSertifikat }, Request.Scheme);

                return Json(new
                {
                    success = true,
                    shareUrl = shareUrl,
                    certificateNumber = certificate.NomorSertifikat,
                    trainingTitle = certificate.Pelatihan.Judul,
                    holderName = certificate.User.Nama
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing certificate {CertificateId}", id);
                return Json(new { success = false, message = "Terjadi kesalahan saat membuat link berbagi" });
            }
        }

        private async Task<byte[]> GenerateCertificatePdf(Sertifikat certificate)
        {
            // This is a placeholder implementation
            // In a real application, you would use a PDF generation library like iTextSharp, DinkToPdf, etc.

            var htmlContent = GenerateCertificateHtml(certificate);

            // For now, return empty byte array
            // In real implementation, convert HTML to PDF
            return Encoding.UTF8.GetBytes(htmlContent);
        }

        private string GenerateCertificateHtml(Sertifikat certificate)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Sertifikat - {certificate.NomorSertifikat}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .certificate {{ border: 5px solid #0066cc; padding: 40px; text-align: center; }}
        .header {{ font-size: 24px; font-weight: bold; color: #0066cc; margin-bottom: 20px; }}
        .title {{ font-size: 32px; font-weight: bold; margin: 20px 0; }}
        .recipient {{ font-size: 20px; margin: 20px 0; }}
        .training {{ font-size: 18px; font-style: italic; margin: 20px 0; }}
        .details {{ margin: 30px 0; }}
        .signature {{ margin-top: 40px; }}
    </style>
</head>
<body>
    <div class='certificate'>
        <div class='header'>PT RAMA EXPRESS</div>
        <div class='title'>SERTIFIKAT PELATIHAN</div>
        <div class='recipient'>
            Diberikan kepada:<br>
            <strong>{certificate.User.Nama}</strong>
        </div>
        <div class='training'>
            Telah berhasil menyelesaikan pelatihan:<br>
            <strong>{certificate.Pelatihan.Judul}</strong>
        </div>
        <div class='details'>
            Nomor Sertifikat: {certificate.NomorSertifikat}<br>
            Tanggal Terbit: {certificate.TanggalTerbit:dd MMMM yyyy}<br>
            Berlaku Hingga: {(certificate.TanggalKadaluarsa == DateTime.MaxValue ? "Selamanya" : certificate.TanggalKadaluarsa.ToString("dd MMMM yyyy"))}
        </div>
        <div class='signature'>
            <br><br>
            _____________________<br>
            Direktur PT Rama Express
        </div>
    </div>
</body>
</html>";
        }
    }
}
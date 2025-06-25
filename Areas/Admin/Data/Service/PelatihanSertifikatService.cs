
// Areas/Admin/Data/Service/PelatihanSertifikatService.cs
using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data.Service
{
    public class PelatihanSertifikatService : IPelatihanSertifikatService
    {
        private readonly RamaExpressAppContext _context;

        public PelatihanSertifikatService(RamaExpressAppContext context)
        {
            _context = context;
        }

        public async Task<PelatihanSertifikat?> GetByPelatihanId(int pelatihanId)
        {
            return await _context.PelatihanSertifikat
                .Include(ps => ps.Pelatihan)
                .FirstOrDefaultAsync(ps => ps.PelatihanId == pelatihanId);
        }

        public async Task<PelatihanSertifikat?> GetById(int id)
        {
            return await _context.PelatihanSertifikat
                .Include(ps => ps.Pelatihan)
                .FirstOrDefaultAsync(ps => ps.Id == id);
        }

        public async Task<(bool Success, string Message, PelatihanSertifikat? Certificate)> Add(PelatihanSertifikat certificate)
        {
            try
            {
                // Check if certificate already exists for this pelatihan
                var existingCertificate = await _context.PelatihanSertifikat
                    .FirstOrDefaultAsync(ps => ps.PelatihanId == certificate.PelatihanId);

                if (existingCertificate != null)
                {
                    return (false, "Sertifikat untuk pelatihan ini sudah ada", null);
                }

                // Validate pelatihan exists
                var pelatihanExists = await _context.Pelatihan
                    .AnyAsync(p => p.Id == certificate.PelatihanId && !p.IsDeleted);

                if (!pelatihanExists)
                {
                    return (false, "Pelatihan tidak ditemukan", null);
                }

                // Set default values
                certificate.CreatedAt = DateTime.Now;
                certificate.UpdatedAt = null;

                // Normalize data
                certificate.TemplateName = certificate.TemplateName.Trim();
                if (!string.IsNullOrEmpty(certificate.TemplateDescription))
                {
                    certificate.TemplateDescription = certificate.TemplateDescription.Trim();
                }

                // Validate expiration settings
                if (certificate.ExpirationType != "never" && !certificate.ExpirationDuration.HasValue)
                {
                    return (false, "Durasi kadaluarsa wajib diisi untuk jenis kadaluarsa yang dipilih", null);
                }

                _context.PelatihanSertifikat.Add(certificate);
                await _context.SaveChangesAsync();

                return (true, $"Pengaturan sertifikat '{certificate.TemplateName}' berhasil ditambahkan", certificate);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, PelatihanSertifikat? Certificate)> Update(PelatihanSertifikat certificate)
        {
            try
            {
                var existingCertificate = await _context.PelatihanSertifikat
                    .FindAsync(certificate.Id);

                if (existingCertificate == null)
                {
                    return (false, "Pengaturan sertifikat tidak ditemukan", null);
                }

                // Update properties
                existingCertificate.IsSertifikatActive = certificate.IsSertifikatActive;
                existingCertificate.TemplateName = certificate.TemplateName.Trim();
                existingCertificate.TemplateDescription = string.IsNullOrEmpty(certificate.TemplateDescription)
                    ? null : certificate.TemplateDescription.Trim();
                existingCertificate.ExpirationType = certificate.ExpirationType;
                existingCertificate.ExpirationDuration = certificate.ExpirationDuration;
                existingCertificate.ExpirationUnit = certificate.ExpirationUnit;
                existingCertificate.CertificateNumberFormat = certificate.CertificateNumberFormat;
                existingCertificate.UpdatedAt = DateTime.Now;

                // Validate expiration settings
                if (existingCertificate.ExpirationType != "never" && !existingCertificate.ExpirationDuration.HasValue)
                {
                    return (false, "Durasi kadaluarsa wajib diisi untuk jenis kadaluarsa yang dipilih", null);
                }

                await _context.SaveChangesAsync();

                return (true, $"Pengaturan sertifikat '{existingCertificate.TemplateName}' berhasil diperbarui", existingCertificate);
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> Delete(int id)
        {
            try
            {
                var certificate = await _context.PelatihanSertifikat.FindAsync(id);
                if (certificate == null)
                {
                    return (false, "Pengaturan sertifikat tidak ditemukan");
                }

                var templateName = certificate.TemplateName;

                // Check if there are any issued certificates
                var issuedCertificates = await _context.Sertifikat
                    .CountAsync(s => s.PelatihanId == certificate.PelatihanId);

                if (issuedCertificates > 0)
                {
                    return (false, $"Tidak dapat menghapus pengaturan sertifikat karena sudah ada {issuedCertificates} sertifikat yang diterbitkan");
                }

                _context.PelatihanSertifikat.Remove(certificate);
                await _context.SaveChangesAsync();

                return (true, $"Pengaturan sertifikat '{templateName}' berhasil dihapus");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        public async Task<string> GenerateCertificateNumber(int pelatihanId)
        {
            try
            {
                var certificateSettings = await GetByPelatihanId(pelatihanId);
                if (certificateSettings == null)
                {
                    // Default format if no settings found
                    var defaultFormat = "CERT-{YEAR}-{MONTH}-{INCREMENT}";
                    return await ProcessCertificateNumberFormat(defaultFormat, pelatihanId);
                }

                return await ProcessCertificateNumberFormat(certificateSettings.CertificateNumberFormat, pelatihanId);
            }
            catch
            {
                // Fallback to simple format
                var now = DateTime.Now;
                var increment = await GetNextIncrement(pelatihanId);
                return $"CERT-{now:yyyy}-{now:MM}-{increment:D4}";
            }
        }

        private async Task<string> ProcessCertificateNumberFormat(string format, int pelatihanId)
        {
            var now = DateTime.Now;
            var processedFormat = format
                .Replace("{YEAR}", now.ToString("yyyy"))
                .Replace("{MONTH}", now.ToString("MM"))
                .Replace("{DAY}", now.ToString("dd"))
                .Replace("{PELATIHAN_ID}", pelatihanId.ToString("D3"));

            // Handle increment placeholder
            if (processedFormat.Contains("{INCREMENT}"))
            {
                var increment = await GetNextIncrement(pelatihanId);
                processedFormat = processedFormat.Replace("{INCREMENT}", increment.ToString("D4"));
            }

            return processedFormat;
        }

        private async Task<int> GetNextIncrement(int pelatihanId)
        {
            var count = await _context.Sertifikat
                .CountAsync(s => s.PelatihanId == pelatihanId);

            return count + 1;
        }
        public async Task<bool> ExistsByPelatihanId(int pelatihanId)
        {
            return await _context.PelatihanSertifikat
                .AnyAsync(ps => ps.PelatihanId == pelatihanId);
        }
    }
}
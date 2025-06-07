using Microsoft.EntityFrameworkCore;
using RamaExpress.Models;

namespace RamaExpress.Data.Service
{
    public class KaryawanService : IKaryawanService
    {
        private readonly RamaExpressAppContext _context;
        public KaryawanService(RamaExpressAppContext context)
        {
            _context = context;
        }
        public async Task Add(Karyawan karyawan)
        {
            _context.Karyawan.Add(karyawan);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Karyawan>> GetAll()
        {
            var karyawan = await _context.Karyawan.ToListAsync();
            return karyawan;
        }
    }
}

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
        public async Task Add(User karyawan)
        {
            _context.User.Add(karyawan);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            var karyawan = await _context.User.ToListAsync();
            return karyawan;
        }
    }
}

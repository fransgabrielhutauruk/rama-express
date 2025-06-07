using Microsoft.EntityFrameworkCore;
using RamaExpress.Models;

namespace RamaExpress.Data
{
    public class RamaExpressAppContext : DbContext
    {
        public RamaExpressAppContext(DbContextOptions<RamaExpressAppContext> options) : base(options) { }
        
        public DbSet<Admin> Admin {  get; set; }
        public DbSet<Karyawan> Karyawan{ get; set; }

    }
}
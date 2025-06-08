using Microsoft.EntityFrameworkCore;
using RamaExpress.Models;

namespace RamaExpress.Data
{
    public class RamaExpressAppContext : DbContext
    {
        public RamaExpressAppContext(DbContextOptions<RamaExpressAppContext> options) : base(options) { }
        
        public DbSet<User> User{ get; set; }

    }
}
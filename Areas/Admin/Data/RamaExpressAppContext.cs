using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data
{
    public class RamaExpressAppContext : DbContext
    {
        public RamaExpressAppContext(DbContextOptions<RamaExpressAppContext> options) : base(options) { }
        
        public DbSet<Models.User> User{ get; set; }

    }
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MVCSampleApp.Models;

namespace MVCSampleApp
{
    // Changed from "DbContext" to "IdentityDbContext<ApplicationUser>" so we get
    // the Identity tables (Users, Roles, Logins, etc.) alongside your existing ones.
    public class AppContext : IdentityDbContext<ApplicationUser>
    {
        public AppContext(DbContextOptions<AppContext> options) : base(options)
        {
        }

        // Keep whatever DbSet properties you already had here, for example:
        public DbSet<Client> Clients { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Service> Services { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // required for Identity to work
        }
    }
}

using Microsoft.EntityFrameworkCore;
using MVCSampleApp.Models;

namespace MVCSampleApp
{
    public class AppContext : DbContext
    {
        public AppContext(DbContextOptions<AppContext> options) : base(options) { }
        public AppContext() { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }
        public DbSet<FullAddress> FullAddresses { get; set; }
        public DbSet<FullName> FullNames { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Employee> Employees { get; set; }

    }

}

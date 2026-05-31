using JobPortal.API.Models;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public DbSet<EmployerProfile> EmployerProfiles { get; set; }
        public DbSet<Company> Companies { get; set; }
    }
}


//This is the bridge between C# models and SQL tables.
//EF Core uses this to create/read/update our DB tables.
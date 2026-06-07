using JobPortal.API.Models;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.API.Data;

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
    public DbSet<CandidateJobMatchCache> CandidateJobMatchCaches { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //we don't want duplicates for same user, job in the cache table
        modelBuilder.Entity<CandidateJobMatchCache>().HasIndex(c => new { c.UserId, c.JobId }).IsUnique();
    }
}


//This is the bridge between C# models and SQL tables.
//EF Core uses this to create/read/update our DB tables.

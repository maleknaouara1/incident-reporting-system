using IncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncidentReporting.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<IncidentHistory> IncidentHistories => Set<IncidentHistory>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
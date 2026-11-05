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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<IncidentAttachment> IncidentAttachments => Set<IncidentAttachment>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // Every timestamp is written as UTC, but SQL Server's datetime2 has no time zone, so EF reads it
    // back as DateTimeKind.Unspecified and the API serializes it without "Z" (browsers then treat it as
    // local time). Mark values read from the database as UTC.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }
}
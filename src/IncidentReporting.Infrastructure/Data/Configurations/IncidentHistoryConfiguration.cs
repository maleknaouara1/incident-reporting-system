using IncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReporting.Infrastructure.Data.Configurations;

public class IncidentHistoryConfiguration : IEntityTypeConfiguration<IncidentHistory>
{
    public void Configure(EntityTypeBuilder<IncidentHistory> builder)
    {
        builder.ToTable("IncidentHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Action)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.OldValue)
            .HasMaxLength(100);

        builder.Property(x => x.NewValue)
            .HasMaxLength(100);

        builder.HasOne(x => x.Incident)
            .WithMany(i => i.Histories)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

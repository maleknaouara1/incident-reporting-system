using IncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReporting.Infrastructure.Data.Configurations;

public class IncidentCommentConfiguration : IEntityTypeConfiguration<IncidentComment>
{
    public void Configure(EntityTypeBuilder<IncidentComment> builder)
    {
        builder.ToTable("IncidentComments");

        builder.HasKey(x => x.Id);

        // All entities in this app assign their own Guid key client-side. Without this,
        // EF's default "Guid key => ValueGeneratedOnAdd" convention makes it misclassify
        // a new comment discovered only via Incident.Comments.Add(...) (not an explicit
        // DbSet.Add) as an existing row to UPDATE instead of a new row to INSERT, which
        // fails with DbUpdateConcurrencyException (0 rows affected).
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(2000);
    }
}

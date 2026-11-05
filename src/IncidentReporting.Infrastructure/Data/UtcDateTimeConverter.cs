using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IncidentReporting.Infrastructure.Data;

// Stores DateTime values as UTC and tags them as UTC when they are read back.
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}

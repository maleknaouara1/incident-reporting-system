using IncidentReporting.Infrastructure.Data;
using Xunit;

namespace IncidentReporting.UnitTests;

public class UtcDateTimeConverterTests
{
    private readonly UtcDateTimeConverter _converter = new();

    [Fact]
    public void ValuesReadFromTheDatabase_AreMarkedAsUtc()
    {
        var stored = new DateTime(2026, 11, 5, 4, 42, 31, DateTimeKind.Unspecified);

        var read = (DateTime)_converter.ConvertFromProvider(stored)!;

        Assert.Equal(DateTimeKind.Utc, read.Kind);
        Assert.Equal(stored.Ticks, read.Ticks);
        Assert.EndsWith("Z", read.ToString("O"));
    }

    [Fact]
    public void UtcValues_AreStoredUnchanged()
    {
        var utc = new DateTime(2026, 11, 5, 4, 42, 31, DateTimeKind.Utc);

        Assert.Equal(utc, (DateTime)_converter.ConvertToProvider(utc)!);
    }

    [Fact]
    public void LocalValues_AreConvertedToUtcBeforeSaving()
    {
        var local = new DateTime(2026, 11, 5, 5, 42, 31, DateTimeKind.Local);

        var stored = (DateTime)_converter.ConvertToProvider(local)!;

        Assert.Equal(local.ToUniversalTime(), stored);
    }
}

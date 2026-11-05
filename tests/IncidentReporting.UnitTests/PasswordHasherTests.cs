using IncidentReporting.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace IncidentReporting.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void VerifyPassword_AcceptsFreshHash()
    {
        var hash = _hasher.HashPassword("Str0ngPassword");

        Assert.True(_hasher.VerifyPassword("Str0ngPassword", hash));
        Assert.False(_hasher.VerifyPassword("WrongPassword1", hash));
    }

    [Fact]
    public void VerifyPassword_AcceptsHashWithFewerIterations()
    {
        // Users registered before the package upgrade have 10,000-iteration hashes
        var legacyHasher = new PasswordHasher<object>(Options.Create(new PasswordHasherOptions { IterationCount = 10_000 }));
        var legacyHash = legacyHasher.HashPassword(new object(), "Str0ngPassword");

        Assert.True(_hasher.VerifyPassword("Str0ngPassword", legacyHash));
        Assert.False(_hasher.VerifyPassword("WrongPassword1", legacyHash));
    }
}

using IncidentReporting.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace IncidentReporting.Infrastructure.Identity;

public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(new object(), password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            new object(),
            passwordHash,
            password);

        // Hashes created with fewer PBKDF2 iterations (e.g. by ASP.NET Core 2.2) are still valid
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
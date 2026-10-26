using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;

namespace IncidentReporting.API.Startup;

// Bootstraps the very first Admin account. Registration always creates a Reporter
// (see AuthService.RegisterAsync), and only an existing Admin can promote other users
// via IUserManagementService, so without this seeder a fresh database would have no
// way to create its first Admin except by writing directly to the database.
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var userRepository = services.GetRequiredService<IUserRepository>();

        if (await userRepository.AnyWithRoleAsync(UserRole.Admin))
            return;

        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            var logger = services.GetRequiredService<ILogger<object>>();
            logger.LogWarning(
                "No Admin user exists and Seed:AdminEmail/Seed:AdminPassword are not configured. " +
                "Set them via user-secrets or environment variables to bootstrap the first Admin account.");
            return;
        }

        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "System Administrator",
            Email = adminEmail.ToLower().Trim(),
            PasswordHash = passwordHasher.HashPassword(adminPassword),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await userRepository.AddAsync(admin);
    }
}

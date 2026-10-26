using IncidentReporting.Application.Auth;
using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Dtos;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReporting.UnitTests;

public class AuthServiceTests
{
    private class InMemoryUserRepository : IUserRepository
    {
        private readonly List<ApplicationUser> _users = new();

        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(x => x.Email == email));
        }

        public Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(x => x.Id == id));
        }

        public Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            var index = _users.FindIndex(x => x.Id == user.Id);
            if (index >= 0) _users[index] = user;
            return Task.CompletedTask;
        }

        public Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.ToList());
        }

        public Task<bool> AnyWithRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.Any(x => x.Role == role));
        }
    }

    private class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = new();

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            _tokens.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tokens.FirstOrDefault(x => x.TokenHash == tokenHash));
        }

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            var index = _tokens.FindIndex(x => x.Id == refreshToken.Id);
            if (index >= 0) _tokens[index] = refreshToken;
            return Task.CompletedTask;
        }

        public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            foreach (var token in _tokens.Where(x => x.UserId == userId && x.RevokedAtUtc is null))
            {
                token.RevokedAtUtc = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }
    }

    private class InMemoryPasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly List<PasswordResetToken> _tokens = new();

        public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
        {
            _tokens.Add(token);
            return Task.CompletedTask;
        }

        public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tokens.FirstOrDefault(x => x.TokenHash == tokenHash));
        }

        public Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
        {
            var index = _tokens.FindIndex(x => x.Id == token.Id);
            if (index >= 0) _tokens[index] = token;
            return Task.CompletedTask;
        }
    }

    private class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class PlainTextPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"HASHED::{password}";

        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"HASHED::{password}";
    }

    private class SequentialJwtTokenGenerator : IJwtTokenGenerator
    {
        private int _refreshTokenCounter;

        public string GenerateToken(ApplicationUser user) => $"access-token-for-{user.Id}";

        public string GenerateRefreshToken() => $"refresh-token-{Interlocked.Increment(ref _refreshTokenCounter)}";
    }

    private static AuthService CreateSut(
        InMemoryUserRepository userRepository,
        InMemoryRefreshTokenRepository refreshTokenRepository)
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Key = "unit-test-signing-key-at-least-32-characters-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        });

        return new AuthService(
            userRepository,
            new PlainTextPasswordHasher(),
            new SequentialJwtTokenGenerator(),
            refreshTokenRepository,
            new InMemoryPasswordResetTokenRepository(),
            new NoOpEmailSender(),
            jwtSettings);
    }

    [Fact]
    public async Task RegisterAsync_AlwaysAssignsReporterRole_RegardlessOfClientInput()
    {
        var sut = CreateSut(new InMemoryUserRepository(), new InMemoryRefreshTokenRepository());

        var response = await sut.RegisterAsync(new RegisterRequest
        {
            FullName = "Jane Doe",
            Email = "jane@example.com",
            Password = "StrongP@ssw0rd"
        });

        Assert.True(response.Success);
        Assert.Equal(UserRole.Reporter, response.Data!.Role);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
    {
        var userRepository = new InMemoryUserRepository();
        var sut = CreateSut(userRepository, new InMemoryRefreshTokenRepository());

        await sut.RegisterAsync(new RegisterRequest { FullName = "Jane", Email = "dup@example.com", Password = "StrongP@ssw0rd" });
        var secondAttempt = await sut.RegisterAsync(new RegisterRequest { FullName = "Jane2", Email = "dup@example.com", Password = "AnotherP@ss1" });

        Assert.False(secondAttempt.Success);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsGenericFailure()
    {
        var sut = CreateSut(new InMemoryUserRepository(), new InMemoryRefreshTokenRepository());

        var response = await sut.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "whatever" });

        Assert.False(response.Success);
        Assert.Equal("Invalid credentials.", response.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAccessAndRefreshToken()
    {
        var userRepository = new InMemoryUserRepository();
        var sut = CreateSut(userRepository, new InMemoryRefreshTokenRepository());
        await sut.RegisterAsync(new RegisterRequest { FullName = "Jane", Email = "jane@example.com", Password = "StrongP@ssw0rd" });

        var response = await sut.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "StrongP@ssw0rd" });

        Assert.True(response.Success);
        Assert.NotEmpty(response.Data!.AccessToken);
        Assert.NotEmpty(response.Data!.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_TooManyFailedAttempts_LocksAccount()
    {
        var userRepository = new InMemoryUserRepository();
        var sut = CreateSut(userRepository, new InMemoryRefreshTokenRepository());
        await sut.RegisterAsync(new RegisterRequest { FullName = "Jane", Email = "jane@example.com", Password = "StrongP@ssw0rd" });

        for (var i = 0; i < 5; i++)
        {
            await sut.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "WrongPassword" });
        }

        // Even the correct password must now be rejected because the account is locked out.
        var response = await sut.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "StrongP@ssw0rd" });

        Assert.False(response.Success);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RotatesAndReturnsNewTokens()
    {
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var sut = CreateSut(userRepository, refreshTokenRepository);
        await sut.RegisterAsync(new RegisterRequest { FullName = "Jane", Email = "jane@example.com", Password = "StrongP@ssw0rd" });
        var login = await sut.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "StrongP@ssw0rd" });

        var refreshed = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.Data!.RefreshToken });

        Assert.True(refreshed.Success);
        Assert.NotEqual(login.Data!.RefreshToken, refreshed.Data!.RefreshToken);

        // The old refresh token must no longer be usable (rotation).
        var reuseOldToken = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.Data!.RefreshToken });
        Assert.False(reuseOldToken.Success);
    }

    [Fact]
    public async Task RevokeTokenAsync_ValidToken_PreventsFurtherUse()
    {
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var sut = CreateSut(userRepository, refreshTokenRepository);
        await sut.RegisterAsync(new RegisterRequest { FullName = "Jane", Email = "jane@example.com", Password = "StrongP@ssw0rd" });
        var login = await sut.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "StrongP@ssw0rd" });

        var revoke = await sut.RevokeTokenAsync(new RefreshTokenRequest { RefreshToken = login.Data!.RefreshToken });
        Assert.True(revoke.Success);

        var refreshAfterRevoke = await sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.Data!.RefreshToken });
        Assert.False(refreshAfterRevoke.Success);
    }
}

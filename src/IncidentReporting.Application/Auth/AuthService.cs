using System.Security.Cryptography;
using System.Text;
using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Dtos;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IncidentReporting.Application.Auth;

public class AuthService : IAuthService
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailSender _emailSender;
    private readonly JwtSettings _jwtSettings;
    private readonly FrontendSettings _frontendSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailSender emailSender,
        IOptions<JwtSettings> jwtSettings,
        IOptions<FrontendSettings> frontendSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailSender = emailSender;
        _jwtSettings = jwtSettings.Value;
        _frontendSettings = frontendSettings.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.ToLower().Trim();

        var existingUser = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (existingUser is not null)
        {
            return new ApiResponse<RegisterResponse>
            {
                Success = false,
                Message = "Email already exists.",
                Errors = new List<string> { "A user with this email already exists." }
            };
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            // Self-registration can never grant anything above the lowest privilege.
            // Promotion to Engineer/Admin must go through IUserManagementService by an existing Admin.
            Role = UserRole.Reporter,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(
            user,
            cancellationToken);

        return new ApiResponse<RegisterResponse>
        {
            Success = true,
            Message = "User registered successfully.",
            Data = new RegisterResponse
            {
                UserId = createdUser.Id,
                FullName = createdUser.FullName,
                Email = createdUser.Email,
                Role = createdUser.Role
            }
        };
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(
            request.Email.ToLower().Trim(),
            cancellationToken);

        // Deliberately generic message below: never reveal whether the email exists,
        // whether the account is locked/inactive, or whether the password was wrong.
        const string genericFailureMessage = "Invalid credentials.";

        if (user is null)
        {
            return new ApiResponse<AuthResponse> { Success = false, Message = genericFailureMessage };
        }

        if (user.LockoutEndUtc is { } lockoutEnd && lockoutEnd > DateTime.UtcNow)
        {
            return new ApiResponse<AuthResponse> { Success = false, Message = genericFailureMessage };
        }

        if (!user.IsActive)
        {
            return new ApiResponse<AuthResponse> { Success = false, Message = genericFailureMessage };
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(
            request.Password,
            user.PasswordHash);

        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }

            await _userRepository.UpdateAsync(user, cancellationToken);

            return new ApiResponse<AuthResponse> { Success = false, Message = genericFailureMessage };
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var authResponse = await IssueTokensAsync(user, cancellationToken);

        return new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Login successful.",
            Data = authResponse
        };
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        const string genericFailureMessage = "Invalid or expired refresh token.";

        var tokenHash = HashToken(request.RefreshToken);

        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return new ApiResponse<AuthResponse> { Success = false, Message = genericFailureMessage };
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new ApiResponse<AuthResponse> { Success = false, Message = genericFailureMessage };
        }

        var authResponse = await IssueTokensAsync(user, cancellationToken);

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = HashToken(authResponse.RefreshToken);
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        return new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Token refreshed successfully.",
            Data = authResponse
        };
    }

    public async Task<ApiResponse<bool>> RevokeTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);

        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Invalid or already revoked refresh token.",
                Data = false
            };
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        return new ApiResponse<bool>
        {
            Success = true,
            Message = "Token revoked successfully.",
            Data = true
        };
    }

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        // Always return the same generic response, whether or not the email exists,
        // so this endpoint can't be used to enumerate registered accounts.
        const string genericMessage = "If an account with that email exists, a password reset link has been sent.";

        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower().Trim(), cancellationToken);

        if (user is not null && user.IsActive)
        {
            var rawToken = _jwtTokenGenerator.GenerateRefreshToken();

            await _passwordResetTokenRepository.AddAsync(
                new Domain.Entities.PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TokenHash = HashToken(rawToken),
                    CreatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.Add(PasswordResetTokenLifetime)
                },
                cancellationToken);

            // The token is Base64, so it must be escaped before going into a query string
            var resetLink = $"{_frontendSettings.BaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";

            try
            {
                await _emailSender.SendAsync(
                    user.Email,
                    "Reset your password",
                    $"Hello {user.FullName},\n\n" +
                    $"Open this link to choose a new password. It expires in {PasswordResetTokenLifetime.TotalMinutes:N0} minutes and works only once:\n\n" +
                    $"{resetLink}\n\n" +
                    "If the link does not open, copy this code into the reset page:\n" +
                    $"{rawToken}\n\n" +
                    "If you did not ask for a password reset, you can ignore this email.",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Still answer with the generic message: a failure only for existing accounts would reveal them
                _logger.LogError(ex, "Could not send the password reset email for user {UserId}.", user.Id);
            }
        }

        return new ApiResponse<bool> { Success = true, Message = genericMessage, Data = true };
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        const string genericFailureMessage = "Invalid or expired reset token.";

        var tokenHash = HashToken(request.Token);

        var storedToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken is null || storedToken.UsedAtUtc is not null || storedToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return new ApiResponse<bool> { Success = false, Message = genericFailureMessage, Data = false };
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new ApiResponse<bool> { Success = false, Message = genericFailureMessage, Data = false };
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        await _userRepository.UpdateAsync(user, cancellationToken);

        storedToken.UsedAtUtc = DateTime.UtcNow;
        await _passwordResetTokenRepository.UpdateAsync(storedToken, cancellationToken);

        // A password reset likely means the old password leaked: kill every existing
        // session rather than leaving already-issued refresh tokens valid.
        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        return new ApiResponse<bool> { Success = true, Message = "Password has been reset successfully.", Data = true };
    }

    private async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = HashToken(refreshToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            },
            cancellationToken);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
        };
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

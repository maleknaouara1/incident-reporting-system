using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Dtos;

namespace IncidentReporting.Application.Auth;

public interface IAuthService
{
    Task<ApiResponse<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> RevokeTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);
}

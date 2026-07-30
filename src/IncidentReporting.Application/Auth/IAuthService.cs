using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Dtos;

namespace IncidentReporting.Application.Auth;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<string>> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken = default);
}
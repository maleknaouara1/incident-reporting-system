using IncidentReporting.API.Auth;
using IncidentReporting.Application.Auth;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
namespace IncidentReporting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IAuthService authService, IOptions<JwtSettings> jwtSettings)
    {
        _authService = authService;
        _jwtSettings = jwtSettings.Value;
    }

    private void SetRefreshTokenCookie(string refreshToken) =>
        Response.Cookies.Append(
            RefreshTokenCookie.Name,
            refreshToken,
            RefreshTokenCookie.Options(Request.IsHttps, DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)));

    private void DeleteRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie.Name, RefreshTokenCookie.Options(Request.IsHttps, expires: null));

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(
            request,
            cancellationToken);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(
            request,
            cancellationToken);

        if (!response.Success)
            return Unauthorized(response);

        SetRefreshTokenCookie(response.Data!.RefreshToken);
        return Ok(response);
    }

    // The refresh token only travels in the HttpOnly cookie, never in the body
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie.Name];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Invalid or expired refresh token."
            });
        }

        var response = await _authService.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = refreshToken },
            cancellationToken);

        if (!response.Success)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(response);
        }

        // Refresh tokens rotate, so the cookie is replaced on every refresh
        SetRefreshTokenCookie(response.Data!.RefreshToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie.Name];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            // Best effort: an unknown or already revoked token still ends the session below
            await _authService.RevokeTokenAsync(
                new RefreshTokenRequest { RefreshToken = refreshToken },
                cancellationToken);
        }

        DeleteRefreshTokenCookie();
        return Ok(new ApiResponse<bool> { Success = true, Message = "Logged out.", Data = true });
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.ForgotPasswordAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.ResetPasswordAsync(
            request,
            cancellationToken);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            FullName = User.FindFirstValue(ClaimTypes.Name),
            Email = User.FindFirstValue(ClaimTypes.Email),
            Role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}

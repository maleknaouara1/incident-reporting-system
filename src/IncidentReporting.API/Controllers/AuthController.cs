using IncidentReporting.Application.Auth;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Dtos;
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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

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

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(
            request,
            cancellationToken);

        if (!response.Success)
            return Unauthorized(response);

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RevokeTokenAsync(
            request,
            cancellationToken);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
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

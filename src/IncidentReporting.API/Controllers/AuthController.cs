using IncidentReporting.Application.Auth;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace IncidentReporting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
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
public async Task<ActionResult<ApiResponse<string>>> Login(
    LoginRequest request,
    CancellationToken cancellationToken)
{
    var response = await _authService.LoginAsync(
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
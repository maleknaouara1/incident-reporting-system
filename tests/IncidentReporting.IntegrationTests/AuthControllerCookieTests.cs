using System.Text.Json;
using IncidentReporting.API.Auth;
using IncidentReporting.API.Controllers;
using IncidentReporting.Application.Auth;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IncidentReporting.IntegrationTests;

public class AuthControllerCookieTests
{
    private class FakeAuthService : IAuthService
    {
        public string IssuedRefreshToken { get; set; } = "refresh-1";
        public string? RefreshedWith { get; private set; }
        public string? RevokedToken { get; private set; }
        public bool RefreshSucceeds { get; set; } = true;

        private AuthResponse Tokens() => new()
        {
            UserId = Guid.NewGuid(),
            FullName = "Jane",
            Email = "jane@example.com",
            AccessToken = "access-token",
            RefreshToken = IssuedRefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(60)
        };

        public Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ApiResponse<AuthResponse> { Success = true, Data = Tokens() });

        public Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            RefreshedWith = request.RefreshToken;
            return Task.FromResult(RefreshSucceeds
                ? new ApiResponse<AuthResponse> { Success = true, Data = Tokens() }
                : new ApiResponse<AuthResponse> { Success = false, Message = "Invalid or expired refresh token." });
        }

        public Task<ApiResponse<bool>> RevokeTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            RevokedToken = request.RefreshToken;
            return Task.FromResult(new ApiResponse<bool> { Success = true, Data = true });
        }

        public Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private static (AuthController Controller, FakeAuthService Service) Create(string? requestCookie = null)
    {
        var service = new FakeAuthService();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        if (requestCookie is not null)
            context.Request.Headers.Cookie = $"{RefreshTokenCookie.Name}={requestCookie}";

        var controller = new AuthController(service, Options.Create(new JwtSettings { RefreshTokenExpiryDays = 7 }))
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
        return (controller, service);
    }

    private static string SetCookieHeader(AuthController controller) =>
        controller.Response.Headers.SetCookie.ToString();

    [Fact]
    public async Task Login_SetsAnHttpOnlyStrictCookie_AndKeepsTheTokenOutOfTheBody()
    {
        var (controller, _) = Create();

        var result = await controller.Login(new LoginRequest { Email = "jane@example.com", Password = "x" }, default);

        var header = SetCookieHeader(controller);
        Assert.Contains($"{RefreshTokenCookie.Name}=refresh-1", header);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", header, StringComparison.OrdinalIgnoreCase);

        var body = JsonSerializer.Serialize(((OkObjectResult)result.Result!).Value);
        Assert.Contains("access-token", body);
        Assert.DoesNotContain("refresh-1", body);
        Assert.DoesNotContain("RefreshToken", body);
    }

    [Fact]
    public async Task Refresh_ReadsTheCookie_AndRotatesIt()
    {
        var (controller, service) = Create(requestCookie: "old-token");
        service.IssuedRefreshToken = "new-token";

        var result = await controller.Refresh(default);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("old-token", service.RefreshedWith);
        Assert.Contains($"{RefreshTokenCookie.Name}=new-token", SetCookieHeader(controller));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401WithoutCallingTheService()
    {
        var (controller, service) = Create();

        var result = await controller.Refresh(default);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Null(service.RefreshedWith);
    }

    [Fact]
    public async Task Refresh_WithARejectedToken_ClearsTheCookie()
    {
        var (controller, service) = Create(requestCookie: "revoked");
        service.RefreshSucceeds = false;

        var result = await controller.Refresh(default);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("expires=Thu, 01 Jan 1970", SetCookieHeader(controller));
    }

    [Fact]
    public async Task Logout_RevokesTheCookieToken_AndClearsIt()
    {
        var (controller, service) = Create(requestCookie: "current");

        var result = await controller.Logout(default);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("current", service.RevokedToken);
        Assert.Contains("expires=Thu, 01 Jan 1970", SetCookieHeader(controller));
    }
}

namespace IncidentReporting.API.Auth;

// The refresh token lives in an HttpOnly cookie so page scripts (and XSS) can never read it.
public static class RefreshTokenCookie
{
    public const string Name = "ir_refresh_token";

    // Only the /api/auth endpoints (refresh, logout) ever receive it
    public const string Path = "/api/auth";

    // SameSite=Strict keeps other sites from making the browser send it (CSRF), which requires
    // the Angular app and the API to be on the same site (same registrable domain).
    public static CookieOptions Options(bool secure, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}

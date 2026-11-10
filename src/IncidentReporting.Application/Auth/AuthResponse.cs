using System.Text.Json.Serialization;
using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Auth;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    // Sent to the browser only as an HttpOnly cookie (see AuthController), never in the JSON body
    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}

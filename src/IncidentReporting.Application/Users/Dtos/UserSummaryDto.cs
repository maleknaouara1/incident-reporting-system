using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Users.Dtos;

public class UserSummaryDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

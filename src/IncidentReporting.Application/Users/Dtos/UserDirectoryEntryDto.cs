using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Users.Dtos;

// Minimal user info shared with all staff (no email or account details)
public class UserDirectoryEntryDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; }
}

using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Users.Dtos;

public class ChangeUserRoleRequest
{
    public UserRole Role { get; set; }
}

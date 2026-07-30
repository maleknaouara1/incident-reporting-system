using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }

    string Email { get; }

    UserRole Role { get; }

    bool IsAuthenticated { get; }
}
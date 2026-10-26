using IncidentReporting.Domain.Entities;

namespace IncidentReporting.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user);

    string GenerateRefreshToken();
}
using IncidentReporting.Domain.Entities;

namespace IncidentReporting.Application.Common.Interfaces.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<ApplicationUser> AddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);
}
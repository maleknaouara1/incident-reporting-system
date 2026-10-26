using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Common.Interfaces.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<ApplicationUser?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApplicationUser> AddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);

    Task<List<ApplicationUser>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> AnyWithRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);
}
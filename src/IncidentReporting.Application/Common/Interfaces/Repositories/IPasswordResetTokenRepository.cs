using IncidentReporting.Domain.Entities;

namespace IncidentReporting.Application.Common.Interfaces.Repositories;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);
}

using IncidentReporting.Domain.Entities;

namespace IncidentReporting.Application.Common.Interfaces.Repositories;

public interface IIncidentRepository
{
    Task<Incident> AddAsync(Incident incident, CancellationToken cancellationToken = default);

    Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Incident>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Incident> UpdateAsync(Incident incident, CancellationToken cancellationToken = default);

Task<List<IncidentHistory>> GetHistoryByIncidentIdAsync(
    Guid incidentId,
    CancellationToken cancellationToken = default);
}
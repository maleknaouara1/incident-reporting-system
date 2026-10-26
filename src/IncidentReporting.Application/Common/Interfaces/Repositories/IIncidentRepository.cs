using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Domain.Entities;

namespace IncidentReporting.Application.Common.Interfaces.Repositories;

public interface IIncidentRepository
{
    Task<Incident> AddAsync(Incident incident, CancellationToken cancellationToken = default);

    Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Incident>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<(List<Incident> Items, int TotalCount)> GetPagedAsync(
        GetIncidentsRequest request,
        CancellationToken cancellationToken = default);

    Task<Incident> UpdateAsync(Incident incident, CancellationToken cancellationToken = default);

    Task<List<IncidentHistory>> GetHistoryByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<Incident?> GetBySourceMessageIdAsync(
        string sourceMessageId,
        CancellationToken cancellationToken = default);
}

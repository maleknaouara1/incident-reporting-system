using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Common.Interfaces;

public interface IN8nService
{
    Task NotifyIncidentCreatedAsync(IncidentDto incident, CancellationToken cancellationToken = default);

    Task NotifyIncidentAssignedAsync(IncidentDto incident, CancellationToken cancellationToken = default);

    Task NotifyIncidentStatusChangedAsync(
        IncidentDto incident,
        IncidentStatus oldStatus,
        CancellationToken cancellationToken = default);
}

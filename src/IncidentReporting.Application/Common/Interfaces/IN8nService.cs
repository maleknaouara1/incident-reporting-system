using IncidentReporting.Application.Incidents.Dtos;

namespace IncidentReporting.Application.Common.Interfaces;

public interface IN8nService
{
    Task NotifyIncidentCreatedAsync(IncidentDto incident, CancellationToken cancellationToken = default);
}

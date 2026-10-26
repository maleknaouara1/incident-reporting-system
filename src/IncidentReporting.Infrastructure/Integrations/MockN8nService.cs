using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IncidentReporting.Infrastructure.Integrations;

public class MockN8nService : IN8nService
{
    private readonly ILogger<MockN8nService> _logger;

    public MockN8nService(ILogger<MockN8nService> logger)
    {
        _logger = logger;
    }

    public Task NotifyIncidentCreatedAsync(IncidentDto incident, CancellationToken cancellationToken = default) =>
        Log("incident.created", incident);

    public Task NotifyIncidentAssignedAsync(IncidentDto incident, CancellationToken cancellationToken = default) =>
        Log("incident.assigned", incident);

    public Task NotifyIncidentStatusChangedAsync(
        IncidentDto incident,
        IncidentStatus oldStatus,
        CancellationToken cancellationToken = default) =>
        Log("incident.statusChanged", new { OldStatus = oldStatus.ToString(), Incident = incident });

    private Task Log(string eventType, object payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("Mock n8n Notification ({EventType}):\n{Payload}", eventType, payloadJson);
        return Task.CompletedTask;
    }
}

using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Incidents.Dtos;
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

    public Task NotifyIncidentCreatedAsync(IncidentDto incident, CancellationToken cancellationToken = default)
    {
        var payloadJson = JsonSerializer.Serialize(incident, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("Mock n8n Notification: Sending incident data to n8n webhook:\n{Payload}", payloadJson);
        return Task.CompletedTask;
    }
}

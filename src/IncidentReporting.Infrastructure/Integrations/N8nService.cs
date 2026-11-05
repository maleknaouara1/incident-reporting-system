using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace IncidentReporting.Infrastructure.Integrations;

public class N8nService : IN8nService
{
    private readonly HttpClient _httpClient;
    private readonly N8nSettings _settings;
    private readonly ILogger<N8nService> _logger;

    public N8nService(HttpClient httpClient, IOptions<N8nSettings> settings, ILogger<N8nService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task NotifyIncidentCreatedAsync(IncidentDto incident, CancellationToken cancellationToken = default) =>
        SendAsync("incident.created", incident, cancellationToken);

    public Task NotifyIncidentAssignedAsync(IncidentDto incident, CancellationToken cancellationToken = default) =>
        SendAsync("incident.assigned", incident, cancellationToken);

    public Task NotifyIncidentStatusChangedAsync(
        IncidentDto incident,
        IncidentStatus oldStatus,
        CancellationToken cancellationToken = default) =>
        SendAsync("incident.statusChanged", new { EventType = "incident.statusChanged", OldStatus = oldStatus.ToString(), Incident = incident }, cancellationToken);

    private async Task SendAsync(string eventType, object incidentPayload, CancellationToken cancellationToken)
    {
        if (!_settings.IsEnabled || string.IsNullOrWhiteSpace(_settings.WebhookUrl))
        {
            _logger.LogInformation("n8n notification ({EventType}) skipped. Webhook is disabled or WebhookUrl is empty.", eventType);
            return;
        }

        if (incidentPayload is IncidentDto incidentDto &&
            incidentDto.Source == IncidentSource.Email &&
            _settings.SkipNotifyForEmailOrigin)
        {
            _logger.LogInformation("n8n notification ({EventType}) skipped for email-origin Incident {IncidentId} to prevent a notification loop.", eventType, incidentDto.Id);
            return;
        }

        var payload = incidentPayload is IncidentDto plainIncident
            ? new { EventType = eventType, Incident = plainIncident }
            : incidentPayload;

        try
        {
            _logger.LogInformation("Sending {EventType} notification to n8n webhook: {Url}", eventType, _settings.WebhookUrl);
            var response = await _httpClient.PostAsJsonAsync(_settings.WebhookUrl, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent {EventType} notification to n8n. Status: {StatusCode}", eventType, response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to send {EventType} notification to n8n. Status: {StatusCode}. Response: {Response}", eventType, response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while notifying n8n webhook for {EventType}.", eventType);
        }
    }
}

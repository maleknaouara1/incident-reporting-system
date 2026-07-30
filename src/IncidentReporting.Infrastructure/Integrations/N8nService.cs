using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Incidents.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

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

    public async Task NotifyIncidentCreatedAsync(IncidentDto incident, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled || string.IsNullOrWhiteSpace(_settings.WebhookUrl))
        {
            _logger.LogInformation("n8n notification skipped. Webhook is disabled or WebhookUrl is empty.");
            return;
        }

        try
        {
            _logger.LogInformation("Sending incident notification to n8n webhook: {Url}", _settings.WebhookUrl);
            var response = await _httpClient.PostAsJsonAsync(_settings.WebhookUrl, incident, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent notification to n8n. Status: {StatusCode}", response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to send notification to n8n. Status: {StatusCode}. Response: {Response}", response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while notifying n8n webhook.");
        }
    }
}

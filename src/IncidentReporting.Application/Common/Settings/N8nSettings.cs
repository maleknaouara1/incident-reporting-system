namespace IncidentReporting.Application.Common.Settings;

public class N8nSettings
{
    public const string SectionName = "n8n";
    public string WebhookUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

namespace IncidentReporting.Application.Common.Settings;

public class AiSettings
{
    public const string SectionName = "Ai";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public bool IsMockEnabled { get; set; } = true;
}

using IncidentReporting.Application.AI.Dtos;

namespace IncidentReporting.Application.Incidents.Dtos;

public class EmailIncidentRequest
{
    public string From { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public AiAnalysisResultDto AiAnalysis { get; set; } = new();
}

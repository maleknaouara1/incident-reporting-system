using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.AI.Dtos;

public class AiAnalysisResultDto
{
    public bool IsIncident { get; set; } = true;
    public string Summary { get; set; } = string.Empty;
    public IncidentCategory SuggestedCategory { get; set; }
    public IncidentPriority SuggestedPriority { get; set; }
    public string? SuggestedResolution { get; set; }
    public double ConfidenceScore { get; set; } = 0.95;
    public string Sentiment { get; set; } = string.Empty;
    public List<string> KeyPhrases { get; set; } = new();
    public bool IsUrgent { get; set; }
}

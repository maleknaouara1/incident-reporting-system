using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.AI.Dtos;

public class AiAnalysisResultDto
{
    public string Summary { get; set; } = string.Empty;
    public IncidentCategory SuggestedCategory { get; set; }
    public IncidentPriority SuggestedPriority { get; set; }
    public string Sentiment { get; set; } = string.Empty;
    public List<string> KeyPhrases { get; set; } = new();
    public bool IsUrgent { get; set; }
}

using IncidentReporting.Application.AI.Dtos;

namespace IncidentReporting.Application.AI;

public interface IAiIncidentAnalyzer
{
    Task<AiAnalysisResultDto> AnalyzeIncidentAsync(string title, string description, CancellationToken cancellationToken = default);
}

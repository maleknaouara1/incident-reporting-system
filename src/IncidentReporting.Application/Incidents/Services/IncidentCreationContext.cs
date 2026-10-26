using IncidentReporting.Application.AI.Dtos;
using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Incidents.Services;

public sealed record IncidentCreationContext(
    Guid CreatedByUserId,
    IncidentSource Source,
    string? SourceMessageId = null,
    AiAnalysisResultDto? PrecomputedAiAnalysis = null);

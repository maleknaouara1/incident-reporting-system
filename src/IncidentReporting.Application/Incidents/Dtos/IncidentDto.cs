using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Incidents.Dtos;

public class IncidentDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IncidentPriority Priority { get; set; }

    public IncidentStatus Status { get; set; }

    public IncidentCategory Category { get; set; }

    public Guid CreatedByUserId { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }
    public string? AiSummary { get; set; }
    public string? AiSuggestedResolution { get; set; }
    public double? AiConfidenceScore { get; set; }
    public List<IncidentCommentDto> Comments { get; set; } = new();
}
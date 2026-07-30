using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Domain.Entities;

public class Incident
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

    public DateTime? ClosedAtUtc { get; set; }

    // AI fields (future)
    public string? AiSummary { get; set; }

    public string? AiSuggestedResolution { get; set; }

    public double? AiConfidenceScore { get; set; }

    // Navigation
    public List<IncidentComment> Comments { get; set; } = new();
    public List<IncidentHistory> Histories { get; set; } = new();
}
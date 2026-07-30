using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Domain.Entities;

public class IncidentHistory
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public IncidentHistoryAction Action { get; set; }
    public string? Description { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    // Navigation
    public Incident Incident { get; set; } = null!;
}

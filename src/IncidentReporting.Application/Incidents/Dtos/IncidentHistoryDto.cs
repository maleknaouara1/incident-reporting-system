using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Incidents.Dtos;

public class IncidentHistoryDto
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public IncidentHistoryAction Action { get; set; }
    public string? Description { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

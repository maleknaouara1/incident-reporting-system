namespace IncidentReporting.Domain.Entities;

public class IncidentComment
{
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid AuthorUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
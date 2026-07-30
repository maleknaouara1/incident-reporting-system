namespace IncidentReporting.Application.Incidents.Dtos;

public class IncidentCommentDto
{
    public Guid Id { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid AuthorUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
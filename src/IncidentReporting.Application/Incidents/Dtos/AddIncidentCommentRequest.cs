namespace IncidentReporting.Application.Incidents.Dtos;

public class AddIncidentCommentRequest
{
    public string Message { get; set; } = string.Empty;

    public Guid AuthorUserId { get; set; }
}
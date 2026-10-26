namespace IncidentReporting.Application.Incidents.Attachments.Dtos;

public class IncidentAttachmentDto
{
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTime UploadedAtUtc { get; set; }
}

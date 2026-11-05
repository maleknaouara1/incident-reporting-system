namespace IncidentReporting.Application.Incidents.Attachments.Dtos;

public class UploadAttachmentRequest
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public Stream Content { get; set; } = Stream.Null;

    public Guid UploadedByUserId { get; set; }
}

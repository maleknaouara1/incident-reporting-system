namespace IncidentReporting.Domain.Entities;

public class IncidentAttachment
{
    public Guid Id { get; set; }

    public Guid IncidentId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }

    public DateTime UploadedAtUtc { get; set; }

    // Navigation
    public Incident Incident { get; set; } = null!;
}

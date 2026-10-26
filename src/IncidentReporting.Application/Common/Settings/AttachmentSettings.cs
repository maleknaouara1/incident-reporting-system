namespace IncidentReporting.Application.Common.Settings;

public class AttachmentSettings
{
    public const string SectionName = "Attachments";

    public string StorageRoot { get; set; } = "App_Data/attachments";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    // Left empty by default (fail closed): the configuration binder appends to a
    // non-empty default array rather than replacing it, so the real allow-list always
    // comes from appsettings.json's Attachments:AllowedExtensions.
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}

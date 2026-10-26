namespace IncidentReporting.Application.Common.Interfaces;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(
        Stream content,
        string suggestedFileName,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}

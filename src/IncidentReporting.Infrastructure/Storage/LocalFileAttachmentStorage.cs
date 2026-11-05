using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace IncidentReporting.Infrastructure.Storage;

public class LocalFileAttachmentStorage : IAttachmentStorage
{
    private readonly AttachmentSettings _settings;

    public LocalFileAttachmentStorage(IOptions<AttachmentSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> SaveAsync(
        Stream content,
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        var rootPath = Path.GetFullPath(_settings.StorageRoot);
        Directory.CreateDirectory(rootPath);

        // Never trust the caller-supplied file name as a path: only its extension is kept,
        // and the actual on-disk name is always a fresh GUID generated here.
        var extension = Path.GetExtension(suggestedFileName);
        var storageKey = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(rootPath, storageKey);

        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);

        return storageKey;
    }

    public async Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var rootPath = Path.GetFullPath(_settings.StorageRoot);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, storagePath));

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved attachment path escapes the storage root.");
        }

        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        return new MemoryStream(bytes);
    }
}

using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Incidents.Attachments.Dtos;

namespace IncidentReporting.Application.Incidents.Attachments;

public interface IIncidentAttachmentService
{
    Task<ApiResponse<IncidentAttachmentDto>> UploadAsync(
        Guid incidentId,
        UploadAttachmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<IncidentAttachmentDto>>> GetByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AttachmentDownloadResult>> DownloadAsync(
        Guid incidentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}

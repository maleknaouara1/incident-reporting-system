using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Incidents.Attachments.Dtos;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;
using Microsoft.Extensions.Options;

namespace IncidentReporting.Application.Incidents.Attachments;

public class IncidentAttachmentService : IIncidentAttachmentService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IIncidentAttachmentRepository _attachmentRepository;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly AttachmentSettings _settings;

    public IncidentAttachmentService(
        IIncidentRepository incidentRepository,
        IIncidentAttachmentRepository attachmentRepository,
        IAttachmentStorage attachmentStorage,
        IOptions<AttachmentSettings> settings)
    {
        _incidentRepository = incidentRepository;
        _attachmentRepository = attachmentRepository;
        _attachmentStorage = attachmentStorage;
        _settings = settings.Value;
    }

    public async Task<ApiResponse<IncidentAttachmentDto>> UploadAsync(
        Guid incidentId,
        UploadAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetByIdAsync(incidentId, cancellationToken);

        if (incident is null)
        {
            return new ApiResponse<IncidentAttachmentDto>
            {
                Success = false,
                Message = "Incident not found.",
                Errors = new List<string> { "No incident exists with the provided id." }
            };
        }

        if (request.SizeBytes <= 0)
        {
            return new ApiResponse<IncidentAttachmentDto>
            {
                Success = false,
                Message = "The uploaded file is empty.",
                Errors = new List<string> { "File size must be greater than zero." }
            };
        }

        if (request.SizeBytes > _settings.MaxFileSizeBytes)
        {
            return new ApiResponse<IncidentAttachmentDto>
            {
                Success = false,
                Message = "The uploaded file is too large.",
                Errors = new List<string>
                {
                    $"Maximum allowed size is {_settings.MaxFileSizeBytes / (1024 * 1024)} MB."
                }
            };
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !_settings.AllowedExtensions.Contains(extension))
        {
            return new ApiResponse<IncidentAttachmentDto>
            {
                Success = false,
                Message = "This file type is not allowed.",
                Errors = new List<string>
                {
                    $"Allowed extensions: {string.Join(", ", _settings.AllowedExtensions)}"
                }
            };
        }

        var storagePath = await _attachmentStorage.SaveAsync(request.Content, request.FileName, cancellationToken);

        var attachment = new IncidentAttachment
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            FileName = Path.GetFileName(request.FileName),
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            SizeBytes = request.SizeBytes,
            StoragePath = storagePath,
            UploadedByUserId = request.UploadedByUserId,
            UploadedAtUtc = DateTime.UtcNow
        };

        await _attachmentRepository.AddAsync(attachment, cancellationToken);

        incident.Histories.Add(new IncidentHistory
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            Action = IncidentHistoryAction.AttachmentAdded,
            Description = $"Attachment '{attachment.FileName}' added by User {request.UploadedByUserId}.",
            ChangedByUserId = request.UploadedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _incidentRepository.UpdateAsync(incident, cancellationToken);

        return new ApiResponse<IncidentAttachmentDto>
        {
            Success = true,
            Message = "Attachment uploaded successfully.",
            Data = MapToDto(attachment)
        };
    }

    public async Task<ApiResponse<List<IncidentAttachmentDto>>> GetByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var attachments = await _attachmentRepository.GetByIncidentIdAsync(incidentId, cancellationToken);

        return new ApiResponse<List<IncidentAttachmentDto>>
        {
            Success = true,
            Message = "Attachments retrieved successfully.",
            Data = attachments.Select(MapToDto).ToList()
        };
    }

    public async Task<ApiResponse<AttachmentDownloadResult>> DownloadAsync(
        Guid incidentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);

        if (attachment is null || attachment.IncidentId != incidentId)
        {
            return new ApiResponse<AttachmentDownloadResult>
            {
                Success = false,
                Message = "Attachment not found.",
                Errors = new List<string> { "No attachment exists with the provided id for this incident." }
            };
        }

        var stream = await _attachmentStorage.OpenReadAsync(attachment.StoragePath, cancellationToken);

        return new ApiResponse<AttachmentDownloadResult>
        {
            Success = true,
            Message = "Attachment ready for download.",
            Data = new AttachmentDownloadResult
            {
                Content = stream,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType
            }
        };
    }

    private static IncidentAttachmentDto MapToDto(IncidentAttachment attachment)
    {
        return new IncidentAttachmentDto
        {
            Id = attachment.Id,
            IncidentId = attachment.IncidentId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
            UploadedByUserId = attachment.UploadedByUserId,
            UploadedAtUtc = attachment.UploadedAtUtc
        };
    }
}

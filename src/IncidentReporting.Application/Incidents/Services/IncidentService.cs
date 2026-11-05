using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Domain.Entities;
using IncidentReporting.Domain.Enums;
using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.AI;
namespace IncidentReporting.Application.Incidents.Services;

public class IncidentService : IIncidentService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiIncidentAnalyzer _aiIncidentAnalyzer;
    private readonly IN8nService _n8nService;
    private readonly IUserRepository _userRepository;

    public IncidentService(
        IIncidentRepository incidentRepository,
        ICurrentUserService currentUserService,
        IAiIncidentAnalyzer aiIncidentAnalyzer,
        IN8nService n8nService,
        IUserRepository userRepository)
    {
        _incidentRepository = incidentRepository;
        _currentUserService = currentUserService;
        _aiIncidentAnalyzer = aiIncidentAnalyzer;
        _n8nService = n8nService;
        _userRepository = userRepository;
    }

    public async Task<ApiResponse<IncidentDto>> CreateAsync(
        CreateIncidentRequest request,
        IncidentCreationContext? creationContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = creationContext ?? new IncidentCreationContext(_currentUserService.UserId, IncidentSource.Manual);

        var aiResult = context.PrecomputedAiAnalysis
            ?? await _aiIncidentAnalyzer.AnalyzeIncidentAsync(request.Title, request.Description, cancellationToken);

        var aiSuggestedResolution = !string.IsNullOrWhiteSpace(aiResult.SuggestedResolution)
            ? aiResult.SuggestedResolution
            : $"Suggested Category: {aiResult.SuggestedCategory}, Priority: {aiResult.SuggestedPriority}. Keywords: {string.Join(", ", aiResult.KeyPhrases)}. Urgent: {aiResult.IsUrgent}";

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Category = request.Category,
            Status = IncidentStatus.New,
            CreatedByUserId = context.CreatedByUserId,
            Source = context.Source,
            SourceMessageId = context.SourceMessageId,
            CreatedAtUtc = DateTime.UtcNow,
            AiSummary = aiResult.Summary,
            AiSuggestedResolution = aiSuggestedResolution,
            AiConfidenceScore = aiResult.ConfidenceScore
        };

        incident.Histories.Add(new IncidentHistory
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Action = IncidentHistoryAction.Created,
            Description = $"Incident created by User {context.CreatedByUserId} (Source: {context.Source}).",
            ChangedByUserId = context.CreatedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        });

        var aiAnalysisDescription = context.PrecomputedAiAnalysis is not null
            ? $"AI Analysis (email/n8n triage) applied. Suggested Category: {aiResult.SuggestedCategory}, Suggested Priority: {aiResult.SuggestedPriority}."
            : $"AI Analysis completed. Suggested Category: {aiResult.SuggestedCategory}, Suggested Priority: {aiResult.SuggestedPriority}.";

        incident.Histories.Add(new IncidentHistory
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Action = IncidentHistoryAction.AiAnalysisGenerated,
            Description = aiAnalysisDescription,
            ChangedByUserId = null,
            CreatedAtUtc = DateTime.UtcNow
        });

        var createdIncident = await _incidentRepository.AddAsync(incident, cancellationToken);
        var dto = MapToDto(createdIncident);

        await _n8nService.NotifyIncidentCreatedAsync(dto, cancellationToken);

        return new ApiResponse<IncidentDto>
        {
            Success = true,
            Message = "Incident created successfully.",
            Data = dto
        };
    }

    public async Task<ApiResponse<PagedResult<IncidentDto>>> GetAllAsync(
        GetIncidentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _incidentRepository.GetPagedAsync(request, cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        return new ApiResponse<PagedResult<IncidentDto>>
        {
            Success = true,
            Message = "Incidents retrieved successfully.",
            Data = new PagedResult<IncidentDto>
            {
                Items = items.Select(MapToDto).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
    }

    public async Task<ApiResponse<IncidentDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetByIdAsync(id, cancellationToken);

        if (incident is null)
        {
            return new ApiResponse<IncidentDto>
            {
                Success = false,
                Message = "Incident not found.",
                Errors = new List<string> { "No incident exists with the provided id." }
            };
        }

        return new ApiResponse<IncidentDto>
        {
            Success = true,
            Message = "Incident retrieved successfully.",
            Data = MapToDto(incident)
        };
    }

public async Task<ApiResponse<IncidentDto>> AssignAsync(
    Guid incidentId,
    AssignIncidentRequest request,
    CancellationToken cancellationToken = default)
{
    var incident = await _incidentRepository.GetByIdAsync(incidentId, cancellationToken);

    if (incident is null)
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Incident not found.",
            Errors = new List<string> { "No incident exists with the provided id." }
        };
    }

    if (incident.Status == IncidentStatus.Closed)
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Cannot assign a closed incident.",
            Errors = new List<string> { "Closed incidents cannot be assigned." }
        };
    }

    // Only active staff can own an incident; never trust the id sent by the client
    var assignee = await _userRepository.GetByIdAsync(request.AssignedToUserId, cancellationToken);

    if (assignee is null || !assignee.IsActive || assignee.Role == UserRole.Reporter)
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Invalid assignee.",
            Errors = new List<string> { "Incidents can only be assigned to an active Engineer or Admin." }
        };
    }

    var oldAssignee = incident.AssignedToUserId;
    incident.AssignedToUserId = assignee.Id;

    // Reassigning keeps the current progress; only a new incident moves to Assigned
    if (incident.Status == IncidentStatus.New)
        incident.Status = IncidentStatus.Assigned;

    incident.UpdatedAtUtc = DateTime.UtcNow;

    incident.Histories.Add(new IncidentHistory
    {
        Id = Guid.NewGuid(),
        IncidentId = incident.Id,
        Action = IncidentHistoryAction.Assigned,
        Description = $"Incident assigned to {assignee.FullName}.",
        OldValue = oldAssignee?.ToString(),
        NewValue = assignee.Id.ToString(),
        ChangedByUserId = _currentUserService.UserId,
        CreatedAtUtc = DateTime.UtcNow
    });

    await _incidentRepository.UpdateAsync(incident, cancellationToken);

    var assignedDto = MapToDto(incident);
    await _n8nService.NotifyIncidentAssignedAsync(assignedDto, cancellationToken);

    return new ApiResponse<IncidentDto>
    {
        Success = true,
        Message = "Incident assigned successfully.",
        Data = assignedDto
    };
}
public async Task<ApiResponse<IncidentDto>> UpdateStatusAsync(
    Guid incidentId,
    UpdateIncidentStatusRequest request,
    CancellationToken cancellationToken = default)
{
    var incident = await _incidentRepository.GetByIdAsync(incidentId, cancellationToken);

    if (incident is null)
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Incident not found.",
            Errors = new List<string> { "No incident exists with the provided id." }
        };
    }

    if (incident.Status == IncidentStatus.Closed)
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Cannot update a closed incident.",
            Errors = new List<string> { "Closed incidents cannot be modified." }
        };
    }

    if (!IsValidTransition(incident.Status, request.Status))
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Invalid status transition.",
            Errors = new List<string>
            {
                $"Cannot move from {incident.Status} to {request.Status}"
            }
        };
    }

    var oldStatus = incident.Status;
    incident.Status = request.Status;
    incident.UpdatedAtUtc = DateTime.UtcNow;

    if (request.Status == IncidentStatus.Resolved)
        incident.ResolvedAtUtc = DateTime.UtcNow;

    if (request.Status == IncidentStatus.Closed)
        incident.ClosedAtUtc = DateTime.UtcNow;

    incident.Histories.Add(new IncidentHistory
    {
        Id = Guid.NewGuid(),
        IncidentId = incident.Id,
        Action = IncidentHistoryAction.StatusChanged,
        Description = $"Incident status updated from {oldStatus} to {request.Status}.",
        OldValue = oldStatus.ToString(),
        NewValue = request.Status.ToString(),
        ChangedByUserId = _currentUserService.UserId,
        CreatedAtUtc = DateTime.UtcNow
    });

    await _incidentRepository.UpdateAsync(incident, cancellationToken);

    var statusChangedDto = MapToDto(incident);
    await _n8nService.NotifyIncidentStatusChangedAsync(statusChangedDto, oldStatus, cancellationToken);

    return new ApiResponse<IncidentDto>
    {
        Success = true,
        Message = "Incident status updated successfully.",
        Data = statusChangedDto
    };
}private static bool IsValidTransition(
    IncidentStatus currentStatus,
    IncidentStatus newStatus)
{
    return currentStatus switch
    {
        IncidentStatus.New => newStatus == IncidentStatus.Assigned,
        IncidentStatus.Assigned => newStatus == IncidentStatus.InProgress,
        IncidentStatus.InProgress => newStatus == IncidentStatus.Resolved,
        IncidentStatus.Resolved => newStatus == IncidentStatus.Closed,
        IncidentStatus.Closed => false,
        _ => false
    };
}
public async Task<ApiResponse<IncidentDto>> AddCommentAsync(
    Guid incidentId,
    AddIncidentCommentRequest request,
    CancellationToken cancellationToken = default)
{
    var incident = await _incidentRepository.GetByIdAsync(incidentId, cancellationToken);

    if (incident is null)
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Incident not found.",
            Errors = new List<string> { "No incident exists with the provided id." }
        };
    }

    if (incident.Status == IncidentStatus.Closed)
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Cannot add comment to a closed incident.",
            Errors = new List<string> { "Closed incidents cannot be modified." }
        };
    }

    var message = request.Message?.Trim() ?? string.Empty;

    if (message.Length == 0 || message.Length > 2000)
    {
        return new ApiResponse<IncidentDto>
        {
            Success = false,
            Message = "Invalid comment.",
            Errors = new List<string> { "A comment must contain between 1 and 2000 characters." }
        };
    }

    // The author is always the signed-in user, so comments can't be posted in someone else's name
    var authorUserId = _currentUserService.UserId;

    incident.Comments.Add(new IncidentComment
    {
        Id = Guid.NewGuid(),
        IncidentId = incident.Id,
        Message = message,
        AuthorUserId = authorUserId,
        CreatedAtUtc = DateTime.UtcNow
    });

    incident.UpdatedAtUtc = DateTime.UtcNow;

    incident.Histories.Add(new IncidentHistory
    {
        Id = Guid.NewGuid(),
        IncidentId = incident.Id,
        Action = IncidentHistoryAction.CommentAdded,
        Description = $"Comment added by User {authorUserId}.",
        ChangedByUserId = _currentUserService.UserId,
        CreatedAtUtc = DateTime.UtcNow
    });

    await _incidentRepository.UpdateAsync(incident, cancellationToken);

    return new ApiResponse<IncidentDto>
    {
        Success = true,
        Message = "Comment added successfully.",
        Data = MapToDto(incident)
    };
}

public async Task<ApiResponse<List<IncidentHistoryDto>>> GetTimelineAsync(
    Guid incidentId,
    CancellationToken cancellationToken = default)
{
    var incident = await _incidentRepository.GetByIdAsync(incidentId, cancellationToken);
    if (incident is null)
    {
        return new ApiResponse<List<IncidentHistoryDto>>
        {
            Success = false,
            Message = "Incident not found.",
            Errors = new List<string> { "No incident exists with the provided id." }
        };
    }

    var histories = await _incidentRepository.GetHistoryByIncidentIdAsync(incidentId, cancellationToken);

    var dtos = histories.Select(h => new IncidentHistoryDto
    {
        Id = h.Id,
        IncidentId = h.IncidentId,
        Action = h.Action,
        Description = h.Description,
        OldValue = h.OldValue,
        NewValue = h.NewValue,
        ChangedByUserId = h.ChangedByUserId,
        CreatedAtUtc = h.CreatedAtUtc
    }).ToList();

    return new ApiResponse<List<IncidentHistoryDto>>
    {
        Success = true,
        Message = "Incident timeline retrieved successfully.",
        Data = dtos
    };
}

    private static IncidentDto MapToDto(Incident incident)
    {
        return new IncidentDto
        {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            Priority = incident.Priority,
            Status = incident.Status,
            Category = incident.Category,
            CreatedByUserId = incident.CreatedByUserId,
            AssignedToUserId = incident.AssignedToUserId,
            Source = incident.Source,
            CreatedAtUtc = incident.CreatedAtUtc,
            UpdatedAtUtc = incident.UpdatedAtUtc,
            ResolvedAtUtc = incident.ResolvedAtUtc,
            AiSummary = incident.AiSummary,
            AiSuggestedResolution = incident.AiSuggestedResolution,
            AiConfidenceScore = incident.AiConfidenceScore,
            Comments = incident.Comments.Select(comment => new IncidentCommentDto
                {
                    Id = comment.Id,
                    Message = comment.Message,
                    AuthorUserId = comment.AuthorUserId,
                    CreatedAtUtc = comment.CreatedAtUtc
                }).ToList()
        };
    }
}
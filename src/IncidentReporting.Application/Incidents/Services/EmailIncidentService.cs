using System.Text.RegularExpressions;
using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Common.Settings;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Domain.Enums;
using IncidentReporting.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IncidentReporting.Application.Incidents.Services;

public class EmailIncidentService : IEmailIncidentService
{
    private const int TitleMaxLength = 200;
    private const int DescriptionMaxLength = 4000;

    private readonly IIncidentService _incidentService;
    private readonly IIncidentRepository _incidentRepository;
    private readonly IUserRepository _userRepository;
    private readonly EmailIntakeSettings _settings;
    private readonly ILogger<EmailIncidentService> _logger;

    public EmailIncidentService(
        IIncidentService incidentService,
        IIncidentRepository incidentRepository,
        IUserRepository userRepository,
        IOptions<EmailIntakeSettings> settings,
        ILogger<EmailIncidentService> logger)
    {
        _incidentService = incidentService;
        _incidentRepository = incidentRepository;
        _userRepository = userRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<IncidentDto>> CreateFromEmailAsync(
        EmailIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var from = request.From.Trim().ToLowerInvariant();
        var subject = CollapseWhitespace(request.Subject);
        var body = request.Body.Trim();
        var messageId = request.MessageId.Trim();

        var existing = await _incidentRepository.GetBySourceMessageIdAsync(messageId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Email intake idempotent replay for MessageId {MessageId} -> Incident {IncidentId}.", messageId, existing.Id);
            return await _incidentService.GetByIdAsync(existing.Id, cancellationToken);
        }

        if (!request.AiAnalysis.IsIncident)
        {
            _logger.LogInformation("Email intake rejected for MessageId {MessageId}: AI triage marked IsIncident=false.", messageId);
            return new ApiResponse<IncidentDto>
            {
                Success = false,
                Message = "The supplied email was not classified as an incident.",
                Errors = new List<string> { "AiAnalysis.IsIncident is false; the n8n triage workflow should not forward non-incident emails to this endpoint." }
            };
        }

        var creatorResult = await ResolveCreatedByUserIdAsync(from, cancellationToken);
        if (!creatorResult.Success)
        {
            return new ApiResponse<IncidentDto>
            {
                Success = false,
                Message = creatorResult.Message,
                Errors = creatorResult.Errors
            };
        }

        var createRequest = new CreateIncidentRequest
        {
            Title = BuildTitle(subject),
            Description = BuildDescription(body, from, request.ReceivedAtUtc),
            Priority = request.AiAnalysis.SuggestedPriority,
            Category = request.AiAnalysis.SuggestedCategory
        };

        var context = new IncidentCreationContext(creatorResult.Data, IncidentSource.Email, messageId, request.AiAnalysis);

        try
        {
            return await _incidentService.CreateAsync(createRequest, context, cancellationToken);
        }
        catch (DuplicateIncidentSourceMessageIdException)
        {
            _logger.LogInformation("Concurrent email intake collision for MessageId {MessageId}. Resolving to existing incident.", messageId);

            var existingAfterRace = await _incidentRepository.GetBySourceMessageIdAsync(messageId, cancellationToken);
            if (existingAfterRace is not null)
                return await _incidentService.GetByIdAsync(existingAfterRace.Id, cancellationToken);

            return new ApiResponse<IncidentDto>
            {
                Success = false,
                Message = "Failed to process duplicate email.",
                Errors = new List<string> { "A conflicting incident could not be resolved." }
            };
        }
    }

    private async Task<ApiResponse<Guid>> ResolveCreatedByUserIdAsync(string from, CancellationToken cancellationToken)
    {
        var sender = await _userRepository.GetByEmailAsync(from, cancellationToken);
        if (sender is not null && sender.IsActive)
        {
            return new ApiResponse<Guid> { Success = true, Data = sender.Id };
        }

        var systemUser = await _userRepository.GetByIdAsync(_settings.SystemUserId, cancellationToken);
        if (systemUser is null || !systemUser.IsActive)
        {
            _logger.LogError("Email intake system user {SystemUserId} does not exist or is inactive.", _settings.SystemUserId);
            return new ApiResponse<Guid>
            {
                Success = false,
                Message = "Email intake is misconfigured.",
                Errors = new List<string> { "The configured system user for email intake does not exist or is inactive." }
            };
        }

        return new ApiResponse<Guid> { Success = true, Data = systemUser.Id };
    }

    private static string BuildTitle(string subject)
    {
        var title = string.IsNullOrWhiteSpace(subject) ? "(No subject)" : subject;
        return title.Length > TitleMaxLength ? title[..TitleMaxLength] : title;
    }

    private static string BuildDescription(string body, string from, DateTime receivedAtUtc)
    {
        var footer = $"\n\n--- Reported via email from {from} at {receivedAtUtc:u} ---";
        var available = Math.Max(DescriptionMaxLength - footer.Length, 0);
        var trimmedBody = body.Length > available ? body[..available] : body;
        return trimmedBody + footer;
    }

    private static string CollapseWhitespace(string input) =>
        Regex.Replace(input.Trim(), @"\s+", " ");
}

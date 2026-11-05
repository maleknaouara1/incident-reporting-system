using IncidentReporting.Application.Common.Interfaces;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Incidents.Attachments;
using IncidentReporting.Application.Incidents.Attachments.Dtos;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Application.Incidents.Services;
using IncidentReporting.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
namespace IncidentReporting.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncidentsController : ControllerBase
{
    // Reporters may create incidents and see their own; triage actions stay with staff.
    private const string StaffRoles = "Admin,Engineer";

    private readonly IIncidentService _incidentService;
    private readonly IIncidentAttachmentService _attachmentService;
    private readonly ICurrentUserService _currentUserService;

    public IncidentsController(
        IIncidentService incidentService,
        IIncidentAttachmentService attachmentService,
        ICurrentUserService currentUserService)
    {
        _incidentService = incidentService;
        _attachmentService = attachmentService;
        _currentUserService = currentUserService;
    }

    private bool IsReporter => _currentUserService.Role == UserRole.Reporter;


    [HttpPost]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> Create(
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _incidentService.CreateAsync(request, cancellationToken: cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<IncidentDto>>>> GetAll(
        [FromQuery] GetIncidentsRequest request,
        CancellationToken cancellationToken)
    {
        // Reporters only ever see the incidents they raised, whatever filter they send.
        if (IsReporter)
            request.CreatedByUserId = _currentUserService.UserId;

        var response = await _incidentService.GetAllAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _incidentService.GetByIdAsync(id, cancellationToken);

        // Report another user's incident as missing so Reporters can't probe for ids.
        if (!response.Success || (IsReporter && response.Data?.CreatedByUserId != _currentUserService.UserId))
            return NotFound(new ApiResponse<IncidentDto> { Success = false, Message = "Incident not found." });

        return Ok(response);
    }
    [HttpPut("{id:guid}/assign")]
    [Authorize(Roles = StaffRoles)]
public async Task<ActionResult<ApiResponse<IncidentDto>>> Assign(
    Guid id,
    AssignIncidentRequest request,
    CancellationToken cancellationToken)
{
    var response = await _incidentService.AssignAsync(id, request, cancellationToken);

    if (!response.Success)
        return BadRequest(response);

    return Ok(response);
}
[HttpPut("{id:guid}/status")]
[Authorize(Roles = StaffRoles)]
public async Task<ActionResult<ApiResponse<IncidentDto>>> UpdateStatus(
    Guid id,
    UpdateIncidentStatusRequest request,
    CancellationToken cancellationToken)
{
    var response = await _incidentService.UpdateStatusAsync(
        id,
        request,
        cancellationToken);

    if (!response.Success)
        return BadRequest(response);

    return Ok(response);
}
[HttpPost("{id:guid}/comments")]
[Authorize(Roles = StaffRoles)]
public async Task<ActionResult<ApiResponse<IncidentDto>>> AddComment(
    Guid id,
    AddIncidentCommentRequest request,
    CancellationToken cancellationToken)
{
    var response = await _incidentService.AddCommentAsync(
        id,
        request,
        cancellationToken);

    if (!response.Success)
        return BadRequest(response);

    return Ok(response);
}

    [HttpGet("{id:guid}/timeline")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<ApiResponse<List<IncidentHistoryDto>>>> GetTimeline(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _incidentService.GetTimelineAsync(id, cancellationToken);

        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    [HttpPost("{id:guid}/attachments")]
    [Authorize(Roles = StaffRoles)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<IncidentAttachmentDto>>> UploadAttachment(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<IncidentAttachmentDto>
            {
                Success = false,
                Message = "No file was provided.",
                Errors = new List<string> { "The 'file' field is required and cannot be empty." }
            });
        }

        await using var stream = file.OpenReadStream();

        var response = await _attachmentService.UploadAsync(
            id,
            new UploadAttachmentRequest
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Content = stream,
                UploadedByUserId = _currentUserService.UserId
            },
            cancellationToken);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("{id:guid}/attachments")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<ApiResponse<List<IncidentAttachmentDto>>>> GetAttachments(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _attachmentService.GetByIncidentIdAsync(id, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/download")]
    [Authorize(Roles = StaffRoles)]
    public async Task<IActionResult> DownloadAttachment(
        Guid id,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var response = await _attachmentService.DownloadAsync(id, attachmentId, cancellationToken);

        if (!response.Success || response.Data is null)
            return NotFound(response);

        return File(response.Data.Content, response.Data.ContentType, response.Data.FileName);
    }
}

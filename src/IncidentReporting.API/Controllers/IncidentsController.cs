using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Application.Incidents.Services;
using Microsoft.AspNetCore.Mvc;
namespace IncidentReporting.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Engineer")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }


    [HttpPost]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> Create(
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _incidentService.CreateAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<IncidentDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await _incidentService.GetAllAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _incidentService.GetByIdAsync(id, cancellationToken);

        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }
    [HttpPut("{id:guid}/assign")]
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
    public async Task<ActionResult<ApiResponse<List<IncidentHistoryDto>>>> GetTimeline(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _incidentService.GetTimelineAsync(id, cancellationToken);

        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }
}
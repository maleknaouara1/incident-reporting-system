using IncidentReporting.API.Filters;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Incidents.Dtos;
using IncidentReporting.Application.Incidents.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReporting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[ServiceFilter(typeof(EmailIntakeApiKeyFilter))]
public class EmailIncidentsController : ControllerBase
{
    private readonly IEmailIncidentService _emailIncidentService;

    public EmailIncidentsController(IEmailIncidentService emailIncidentService)
    {
        _emailIncidentService = emailIncidentService;
    }

    [HttpPost("inbound")]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> Inbound(
        EmailIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _emailIncidentService.CreateFromEmailAsync(request, cancellationToken);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}

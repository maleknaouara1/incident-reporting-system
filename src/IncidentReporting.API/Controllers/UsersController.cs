using System.Security.Claims;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Users;
using IncidentReporting.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReporting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserSummaryDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await _userManagementService.GetAllAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> ChangeRole(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _userManagementService.ChangeRoleAsync(id, request, cancellationToken);

        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> SetStatus(
        Guid id,
        SetUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var requestingUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var response = await _userManagementService.SetActiveStatusAsync(id, request, requestingUserId, cancellationToken);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}

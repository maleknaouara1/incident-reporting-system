using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Users;
using IncidentReporting.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReporting.API.Controllers;

// Separate from UsersController, whose class-level policy is Admin only:
// Engineers need names to read incidents, comments and history.
[ApiController]
[Route("api/users/directory")]
[Authorize(Roles = "Admin,Engineer")]
public class UserDirectoryController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserDirectoryController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDirectoryEntryDto>>>> Get(
        CancellationToken cancellationToken)
    {
        var response = await _userManagementService.GetDirectoryAsync(cancellationToken);

        return Ok(response);
    }
}

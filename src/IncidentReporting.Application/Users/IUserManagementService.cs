using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Users.Dtos;

namespace IncidentReporting.Application.Users;

public interface IUserManagementService
{
    Task<ApiResponse<List<UserSummaryDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserSummaryDto>> ChangeRoleAsync(
        Guid userId,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserSummaryDto>> SetActiveStatusAsync(
        Guid userId,
        SetUserStatusRequest request,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);
}

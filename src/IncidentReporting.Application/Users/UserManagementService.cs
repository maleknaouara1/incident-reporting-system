using IncidentReporting.Application.Common.Interfaces.Repositories;
using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Users.Dtos;

namespace IncidentReporting.Application.Users;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;

    public UserManagementService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ApiResponse<List<UserSummaryDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        return new ApiResponse<List<UserSummaryDto>>
        {
            Success = true,
            Message = "Users retrieved successfully.",
            Data = users.Select(MapToDto).ToList()
        };
    }

    public async Task<ApiResponse<List<UserDirectoryEntryDto>>> GetDirectoryAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        return new ApiResponse<List<UserDirectoryEntryDto>>
        {
            Success = true,
            Message = "User directory retrieved successfully.",
            Data = users
                .OrderBy(user => user.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(user => new UserDirectoryEntryDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive
                })
                .ToList()
        };
    }

    public async Task<ApiResponse<UserSummaryDto>> ChangeRoleAsync(
        Guid userId,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return new ApiResponse<UserSummaryDto>
            {
                Success = false,
                Message = "User not found.",
                Errors = new List<string> { "No user exists with the given id." }
            };
        }

        user.Role = request.Role;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new ApiResponse<UserSummaryDto>
        {
            Success = true,
            Message = "User role updated successfully.",
            Data = MapToDto(user)
        };
    }

    public async Task<ApiResponse<UserSummaryDto>> SetActiveStatusAsync(
        Guid userId,
        SetUserStatusRequest request,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsActive && userId == requestingUserId)
        {
            return new ApiResponse<UserSummaryDto>
            {
                Success = false,
                Message = "You cannot deactivate your own account.",
                Errors = new List<string> { "Self-deactivation is not allowed." }
            };
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return new ApiResponse<UserSummaryDto>
            {
                Success = false,
                Message = "User not found.",
                Errors = new List<string> { "No user exists with the given id." }
            };
        }

        user.IsActive = request.IsActive;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new ApiResponse<UserSummaryDto>
        {
            Success = true,
            Message = request.IsActive ? "User activated successfully." : "User deactivated successfully.",
            Data = MapToDto(user)
        };
    }

    private static UserSummaryDto MapToDto(Domain.Entities.ApplicationUser user)
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }
}

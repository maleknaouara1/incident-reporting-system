using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Incidents.Dtos;

namespace IncidentReporting.Application.Incidents.Services;

public interface IIncidentService
{
    Task<ApiResponse<IncidentDto>> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<IncidentDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IncidentDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IncidentDto>> AssignAsync(
        Guid incidentId,
        AssignIncidentRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IncidentDto>> UpdateStatusAsync(
    Guid incidentId,
    UpdateIncidentStatusRequest request,
    CancellationToken cancellationToken = default);
    Task<ApiResponse<IncidentDto>> AddCommentAsync(
    Guid incidentId,
    AddIncidentCommentRequest request,
    CancellationToken cancellationToken = default);

    Task<ApiResponse<List<IncidentHistoryDto>>> GetTimelineAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}
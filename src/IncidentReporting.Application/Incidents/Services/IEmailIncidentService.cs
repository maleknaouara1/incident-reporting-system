using IncidentReporting.Application.Common.Models;
using IncidentReporting.Application.Incidents.Dtos;

namespace IncidentReporting.Application.Incidents.Services;

public interface IEmailIncidentService
{
    Task<ApiResponse<IncidentDto>> CreateFromEmailAsync(
        EmailIncidentRequest request,
        CancellationToken cancellationToken = default);
}

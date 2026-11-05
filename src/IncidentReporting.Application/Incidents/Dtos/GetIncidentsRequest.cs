using IncidentReporting.Application.Common.Models;
using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Incidents.Dtos;

public class GetIncidentsRequest : PagedRequest
{
    public IncidentStatus? Status { get; set; }

    public IncidentPriority? Priority { get; set; }

    public IncidentCategory? Category { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public string? Search { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; } = true;
}

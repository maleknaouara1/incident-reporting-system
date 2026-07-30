using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Incidents.Dtos;

public class CreateIncidentRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IncidentPriority Priority { get; set; }

    public IncidentCategory Category { get; set; }

}
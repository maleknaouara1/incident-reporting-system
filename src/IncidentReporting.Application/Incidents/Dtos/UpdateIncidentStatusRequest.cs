using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Incidents.Dtos;

public class UpdateIncidentStatusRequest
{
    public IncidentStatus Status { get; set; }
}
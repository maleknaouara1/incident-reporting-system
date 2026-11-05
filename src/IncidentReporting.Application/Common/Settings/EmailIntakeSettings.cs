using IncidentReporting.Domain.Enums;

namespace IncidentReporting.Application.Common.Settings;

public class EmailIntakeSettings
{
    public const string SectionName = "EmailIntake";

    public bool IsEnabled { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public Guid SystemUserId { get; set; }

    public IncidentCategory DefaultCategory { get; set; } = IncidentCategory.Other;

    public IncidentPriority DefaultPriority { get; set; } = IncidentPriority.Medium;
}

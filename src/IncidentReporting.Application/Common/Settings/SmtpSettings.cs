namespace IncidentReporting.Application.Common.Settings;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public bool IsMockEnabled { get; set; } = true;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = "no-reply@incidentreporting.local";

    public string FromName { get; set; } = "Incident Reporting System";
}

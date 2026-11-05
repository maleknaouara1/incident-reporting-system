namespace IncidentReporting.Application.Common.Settings;

public class FrontendSettings
{
    public const string SectionName = "Frontend";

    // Public origin of the Angular app: used for links in emails and as the allowed CORS origin
    public string BaseUrl { get; set; } = "http://localhost:4200";
}

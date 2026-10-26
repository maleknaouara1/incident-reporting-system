using System.Security.Cryptography;
using System.Text;
using IncidentReporting.Application.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace IncidentReporting.API.Filters;

public class EmailIntakeApiKeyFilter : IAsyncActionFilter
{
    private const string HeaderName = "X-Email-Intake-Key";

    private readonly EmailIntakeSettings _settings;
    private readonly ILogger<EmailIntakeApiKeyFilter> _logger;

    public EmailIntakeApiKeyFilter(
        IOptions<EmailIntakeSettings> settings,
        ILogger<EmailIntakeApiKeyFilter> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_settings.IsEnabled)
        {
            _logger.LogInformation("Email intake request rejected: feature is disabled.");
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            return;
        }

        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (!IsValidKey(providedKey))
        {
            _logger.LogWarning("Email intake request rejected: invalid or missing API key.");
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }

    private bool IsValidKey(string providedKey)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey) || string.IsNullOrEmpty(providedKey))
            return false;

        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        var expectedBytes = Encoding.UTF8.GetBytes(_settings.ApiKey);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}

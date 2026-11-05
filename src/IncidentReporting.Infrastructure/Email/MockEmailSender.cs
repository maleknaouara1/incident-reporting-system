using IncidentReporting.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IncidentReporting.Infrastructure.Email;

public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Mock email (no SMTP configured): To={ToEmail}, Subject={Subject}\n{Body}",
            toEmail,
            subject,
            body);

        return Task.CompletedTask;
    }
}

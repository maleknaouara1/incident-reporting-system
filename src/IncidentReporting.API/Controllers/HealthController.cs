using Microsoft.AspNetCore.Mvc;

namespace IncidentReporting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Application = "AI Incident Reporting API",
            Version = "1.0.0",
            CheckedAtUtc = DateTime.UtcNow
        });
    }
}
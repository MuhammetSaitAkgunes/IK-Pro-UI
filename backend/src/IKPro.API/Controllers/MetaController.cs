using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace IKPro.API.Controllers;

/// <summary>
/// Lightweight, unauthenticated endpoints for smoke-testing that the API is up.
/// </summary>
[ApiController]
[Route("api")]
public sealed class MetaController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new
    {
        service = "İK Pro API",
        status = "ok",
        version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
        timeUtc = DateTime.UtcNow
    });
}

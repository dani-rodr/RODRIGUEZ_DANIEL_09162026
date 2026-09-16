using Microsoft.AspNetCore.Mvc;

namespace FileProcessing.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public HealthResponse Get() => new("ok");
}

public sealed record HealthResponse(string Status);

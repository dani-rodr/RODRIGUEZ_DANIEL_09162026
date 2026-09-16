using FileProcessing.Api.Authentication;
using FileProcessing.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileProcessing.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    [HttpPost("process")]
    [ProducesResponseType<ProcessResponse>(StatusCodes.Status200OK)]
    public ProcessResponse Process(
        [FromHeader(Name = ApiKeyMiddleware.HeaderName)] string apiKey) => new(true);
}

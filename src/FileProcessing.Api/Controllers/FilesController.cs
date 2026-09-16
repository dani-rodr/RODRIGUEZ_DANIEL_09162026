using FileProcessing.Api.Authentication;
using FileProcessing.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileProcessing.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UploadResponse>(StatusCodes.Status200OK)]
    public UploadResponse Process(
        [FromHeader(Name = ApiKeyMiddleware.HeaderName)] string apiKey,
        IFormFile file) => new(file.FileName, file.Length);
}

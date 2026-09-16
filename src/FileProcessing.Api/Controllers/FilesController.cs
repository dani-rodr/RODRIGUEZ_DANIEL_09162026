using FileProcessing.Api.Authentication;
using FileProcessing.Api.Data;
using FileProcessing.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FileProcessing.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(IFileStore fileStore) : ControllerBase
{
    [HttpGet("report")]
    [ProducesResponseType<FileReportResponse>(StatusCodes.Status200OK)]
    public async Task<FileReportResponse> Report(
        [FromHeader(Name = ApiKeyMiddleware.HeaderName)] string apiKey,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FileReportItem> files = await fileStore.GetReportAsync(cancellationToken);
        return new FileReportResponse(files.Count, files);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UploadResponse>(StatusCodes.Status200OK)]
    public async Task<UploadResponse> Upload(
        [FromHeader(Name = ApiKeyMiddleware.HeaderName)] string apiKey,
        IFormFile file)
    {
        await using Stream stream = file.OpenReadStream();
        List<ItemRecord> records = await JsonSerializer.DeserializeAsync<List<ItemRecord>>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];

        StoredFile storedFile = new()
        {
            FileName = file.FileName,
            Size = file.Length,
            UploadedAtUtc = DateTime.UtcNow,
            Records = records
        };

        await fileStore.SaveAsync(storedFile);

        return new UploadResponse(
            storedFile.Id,
            storedFile.FileName,
            storedFile.Size,
            storedFile.Records.Count);
    }
}

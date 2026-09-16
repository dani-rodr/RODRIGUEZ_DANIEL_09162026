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
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponse>> Upload(
        [FromHeader(Name = ApiKeyMiddleware.HeaderName)] string apiKey,
        IFormFile file)
    {
        if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".json",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only JSON files are allowed.");
        }

        List<ItemRecord>? records;

        await using Stream stream = file.OpenReadStream();

        try
        {
            records = await JsonSerializer.DeserializeAsync<List<ItemRecord>>(
                stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    RespectNullableAnnotations = true,
                    RespectRequiredConstructorParameters = true
                });
        }
        catch (JsonException)
        {
            return BadRequest("Invalid JSON structure.");
        }

        if (records is null || records.Any(record => record is null || record.Name is null))
        {
            return BadRequest("Invalid JSON structure.");
        }


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

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

    [HttpGet("{id}/records")]
    [ProducesResponseType<FilteredRecordsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FilteredRecordsResponse>> Records(
        [FromHeader(Name = ApiKeyMiddleware.HeaderName)] string apiKey,
        string id,
        [FromQuery] RecordFilterQuery filter,
        CancellationToken cancellationToken)
    {
        if (filter.Active is null && filter.ActiveComparison is not null)
        {
            return BadRequest("activeComparison requires active.");
        }

        if (filter.Name is null && filter.NameComparison is not null)
        {
            return BadRequest("nameComparison requires name.");
        }

        if (filter.Value is null && filter.ValueComparison is not null)
        {
            return BadRequest("valueComparison requires value.");
        }

        if (filter.ActiveComparison is not null &&
            !Enum.IsDefined(filter.ActiveComparison.Value))
        {
            return BadRequest("Invalid activeComparison.");
        }

        if (filter.NameComparison is not null &&
            !Enum.IsDefined(filter.NameComparison.Value))
        {
            return BadRequest("Invalid nameComparison.");
        }

        if (filter.ValueComparison is not null &&
            !Enum.IsDefined(filter.ValueComparison.Value))
        {
            return BadRequest("Invalid valueComparison.");
        }

        StoredFile? file = await fileStore.GetAsync(id, cancellationToken);
        if (file is null)
        {
            return NotFound("File not found.");
        }

        IEnumerable<ItemRecord> records = file.Records;

        if (filter.Active is not null)
        {
            ActiveComparison comparison = filter.ActiveComparison ?? ActiveComparison.Equal;
            records = comparison switch
            {
                ActiveComparison.Equal => records.Where(record => record.Active == filter.Active),
                ActiveComparison.NotEqual => records.Where(record => record.Active != filter.Active),
                _ => throw new ArgumentOutOfRangeException(nameof(filter.ActiveComparison))
            };
        }

        if (filter.Name is not null)
        {
            NameComparison comparison = filter.NameComparison ?? NameComparison.Contains;
            records = comparison switch
            {
                NameComparison.Equal => records.Where(record =>
                    string.Equals(record.Name, filter.Name, StringComparison.OrdinalIgnoreCase)),
                NameComparison.Contains => records.Where(record =>
                    record.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase)),
                NameComparison.StartsWith => records.Where(record =>
                    record.Name.StartsWith(filter.Name, StringComparison.OrdinalIgnoreCase)),
                _ => throw new ArgumentOutOfRangeException(nameof(filter.NameComparison))
            };
        }

        if (filter.Value is not null)
        {
            ValueComparison comparison = filter.ValueComparison ?? ValueComparison.Equal;
            records = comparison switch
            {
                ValueComparison.Equal => records.Where(record => record.Value == filter.Value),
                ValueComparison.GreaterThan => records.Where(record => record.Value > filter.Value),
                ValueComparison.GreaterThanOrEqual => records.Where(record =>
                    record.Value >= filter.Value),
                ValueComparison.LessThan => records.Where(record => record.Value < filter.Value),
                ValueComparison.LessThanOrEqual => records.Where(record =>
                    record.Value <= filter.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(filter.ValueComparison))
            };
        }

        List<ItemRecord> matchedRecords = records.ToList();
        return new FilteredRecordsResponse(
            file.Id,
            file.FileName,
            file.Records.Count,
            matchedRecords.Count,
            matchedRecords);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromHeader(Name = ApiKeyMiddleware.HeaderName)] string apiKey,
        string id,
        CancellationToken cancellationToken)
    {
        bool deleted = await fileStore.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound("File not found.");
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponse>> Upload(
        [FromHeader(Name = ApiKeyMiddleware.HeaderName)] string apiKey,
        IFormFile? file)
    {
        if (file is null)
        {
            return BadRequest("A JSON file is required.");
        }

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

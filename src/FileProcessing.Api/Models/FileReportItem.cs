namespace FileProcessing.Api.Models;

public sealed class FileReportItem
{
    public string Id { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public long Size { get; init; }

    public DateTime UploadedAtUtc { get; init; }

    public int RecordCount { get; init; }
}

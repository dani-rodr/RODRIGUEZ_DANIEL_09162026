namespace FileProcessing.Api.Models;

public sealed record FileReportResponse(int TotalFiles, IReadOnlyList<FileReportItem> Files);

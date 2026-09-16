namespace FileProcessing.Api.Models;

public sealed record UploadResponse(string Id, string FileName, long Size, int RecordCount);

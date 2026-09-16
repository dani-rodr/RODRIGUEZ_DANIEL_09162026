namespace FileProcessing.Api.Models;

public sealed record FilteredRecordsResponse(
    string FileId,
    string FileName,
    int TotalCount,
    int MatchedCount,
    IReadOnlyList<ItemRecord> Records);

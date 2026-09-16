using MongoDB.Bson.Serialization.Attributes;

namespace FileProcessing.Api.Models;

public sealed class StoredFile
{
    [BsonId]
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string FileName { get; init; } = string.Empty;

    public long Size { get; init; }

    public DateTime UploadedAtUtc { get; init; }

    public List<ItemRecord> Records { get; init; } = [];
}

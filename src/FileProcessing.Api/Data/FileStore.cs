using FileProcessing.Api.Models;
using MongoDB.Driver;

namespace FileProcessing.Api.Data;

public interface IFileStore
{
    Task SaveAsync(StoredFile file);

    Task<StoredFile?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileReportItem>> GetReportAsync(
        CancellationToken cancellationToken = default);
}

public sealed class MongoFileStore(IConfiguration configuration) : IFileStore
{
    private readonly IMongoCollection<StoredFile> files = new MongoClient(
        configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017")
        .GetDatabase(configuration["MongoDb:DatabaseName"] ?? "file-processing")
        .GetCollection<StoredFile>("files");

    public Task SaveAsync(StoredFile file) => files.InsertOneAsync(file);

    public async Task<StoredFile?> GetAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        await files.Find(file => file.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FileReportItem>> GetReportAsync(
        CancellationToken cancellationToken = default)
    {
        return await files.Aggregate()
            .Project(file => new FileReportItem
            {
                Id = file.Id,
                FileName = file.FileName,
                Size = file.Size,
                UploadedAtUtc = file.UploadedAtUtc,
                RecordCount = file.Records.Count
            })
            .SortByDescending(file => file.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

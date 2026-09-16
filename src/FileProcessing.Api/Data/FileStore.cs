using FileProcessing.Api.Models;
using MongoDB.Driver;

namespace FileProcessing.Api.Data;

public interface IFileStore
{
    Task SaveAsync(StoredFile file);
}

public sealed class MongoFileStore(IConfiguration configuration) : IFileStore
{
    private readonly IMongoCollection<StoredFile> files = new MongoClient(
        configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017")
        .GetDatabase(configuration["MongoDb:DatabaseName"] ?? "file-processing")
        .GetCollection<StoredFile>("files");

    public Task SaveAsync(StoredFile file) => files.InsertOneAsync(file);
}

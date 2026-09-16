using FileProcessing.Api.Data;
using FileProcessing.Api.Controllers;
using FileProcessing.Api.Models;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace FileProcessing.Api.Tests;

public sealed class FilesControllerTests
{
    [Fact]
    public async Task Upload_StoresUploadedRecordsAndReturnsMetadata()
    {
        InMemoryFileStore store = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(
            "[{\"id\":1,\"name\":\"Item A\",\"active\":true,\"value\":75}]"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "items.json");

        UploadResponse response = await new FilesController(store).Upload("test-key", file);

        Assert.NotNull(store.SavedFile);
        Assert.Equal("items.json", response.FileName);
        Assert.Equal(stream.Length, response.Size);
        Assert.Equal(1, response.RecordCount);
        Assert.Equal("Item A", store.SavedFile!.Records[0].Name);
        Assert.Equal(75, store.SavedFile.Records[0].Value);
    }

    [Fact]
    public async Task Report_WhenNoFilesExist_ReturnsEmptyReport()
    {
        FileReportResponse response = await new FilesController(new InMemoryFileStore())
            .Report("test-key", CancellationToken.None);

        Assert.Equal(0, response.TotalFiles);
        Assert.Empty(response.Files);
    }

    [Fact]
    public async Task Report_ReturnsStoredFileMetadata()
    {
        DateTime firstUpload = new(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        DateTime secondUpload = firstUpload.AddMinutes(5);
        InMemoryFileStore store = new()
        {
            Report =
            [
                new FileReportItem
                {
                    Id = "second-file",
                    FileName = "second.json",
                    Size = 200,
                    UploadedAtUtc = secondUpload,
                    RecordCount = 2
                },
                new FileReportItem
                {
                    Id = "first-file",
                    FileName = "first.json",
                    Size = 100,
                    UploadedAtUtc = firstUpload,
                    RecordCount = 1
                }
            ]
        };

        FileReportResponse response = await new FilesController(store)
            .Report("test-key", CancellationToken.None);

        Assert.Equal(2, response.TotalFiles);
        Assert.Collection(
            response.Files,
            file =>
            {
                Assert.Equal("second-file", file.Id);
                Assert.Equal("second.json", file.FileName);
                Assert.Equal(200, file.Size);
                Assert.Equal(secondUpload, file.UploadedAtUtc);
                Assert.Equal(2, file.RecordCount);
            },
            file => Assert.Equal("first-file", file.Id));
    }

    private sealed class InMemoryFileStore : IFileStore
    {
        public StoredFile? SavedFile { get; set; }

        public IReadOnlyList<FileReportItem> Report { get; set; } = [];

        public Task SaveAsync(StoredFile file)
        {
            SavedFile = file;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FileReportItem>> GetReportAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(Report);

    }
}

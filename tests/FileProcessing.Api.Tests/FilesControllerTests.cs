using FileProcessing.Api.Controllers;
using FileProcessing.Api.Data;
using FileProcessing.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        ActionResult<UploadResponse> result =
            await new FilesController(store).Upload("test-key", file);

        UploadResponse response = Assert.IsType<UploadResponse>(result.Value);

        Assert.NotNull(store.SavedFile);
        Assert.Equal("items.json", response.FileName);
        Assert.Equal(stream.Length, response.Size);
        Assert.Equal(1, response.RecordCount);
        Assert.Equal("Item A", store.SavedFile!.Records[0].Name);
        Assert.Equal(75, store.SavedFile.Records[0].Value);
    }

    [Fact]
    public async Task Upload_WhenFileIsNotJson_ReturnsBadRequest()
    {
        InMemoryFileStore store = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("test"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "items.txt");

        ActionResult<UploadResponse> result =
            await new FilesController(store).Upload("test-key", file);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Only JSON files are allowed.", badRequest.Value);
        Assert.Null(store.SavedFile);
    }

    [Fact]
    public async Task Upload_WhenJsonIsMalformed_ReturnsBadRequest()
    {
        InMemoryFileStore store = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(
            "[{\"id\":1,\"name\":\"Item A\""));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "items.json");

        ActionResult<UploadResponse> result =
            await new FilesController(store).Upload("test-key", file);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Invalid JSON structure.", badRequest.Value);
        Assert.Null(store.SavedFile);
    }

    [Fact]
    public async Task Upload_WhenJsonHasInvalidFieldType_ReturnsBadRequest()
    {
        InMemoryFileStore store = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(
            "[{\"id\":\"invalid\",\"name\":\"Item A\",\"active\":true,\"value\":75}]"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "items.json");

        ActionResult<UploadResponse> result =
            await new FilesController(store).Upload("test-key", file);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Invalid JSON structure.", badRequest.Value);
        Assert.Null(store.SavedFile);
    }

    [Fact]
    public async Task Upload_WhenJsonRecordIsMissingRequiredField_ReturnsBadRequest()
    {
        InMemoryFileStore store = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(
            "[{\"id\":1,\"active\":true,\"value\":75}]"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "items.json");

        ActionResult<UploadResponse> result =
            await new FilesController(store).Upload("test-key", file);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Invalid JSON structure.", badRequest.Value);
        Assert.Null(store.SavedFile);
    }

    [Fact]
    public async Task Upload_WhenJsonContainsNullRecord_ReturnsBadRequest()
    {
        InMemoryFileStore store = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("[null]"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "items.json");

        ActionResult<UploadResponse> result =
            await new FilesController(store).Upload("test-key", file);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Invalid JSON structure.", badRequest.Value);
        Assert.Null(store.SavedFile);
    }

    [Fact]
    public async Task Upload_WhenJsonIsNull_ReturnsBadRequest()
    {
        InMemoryFileStore store = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("null"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "items.json");

        ActionResult<UploadResponse> result =
            await new FilesController(store).Upload("test-key", file);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Invalid JSON structure.", badRequest.Value);
        Assert.Null(store.SavedFile);
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

    [Fact]
    public async Task Records_WithoutFilters_ReturnsAllStoredRecords()
    {
        InMemoryFileStore store = new() { RetrievedFile = CreateStoredFile() };

        ActionResult<FilteredRecordsResponse> result = await new FilesController(store)
            .Records("test-key", "file-1", new(), CancellationToken.None);

        FilteredRecordsResponse response = Assert.IsType<FilteredRecordsResponse>(result.Value);
        Assert.Equal("file-1", response.FileId);
        Assert.Equal("items.json", response.FileName);
        Assert.Equal(10, response.TotalCount);
        Assert.Equal(10, response.MatchedCount);
        Assert.Equal(10, response.Records.Count);
    }

    [Fact]
    public async Task Records_CombinesActiveNameAndValueFilters()
    {
        InMemoryFileStore store = new() { RetrievedFile = CreateStoredFile() };
        RecordFilterQuery filter = new()
        {
            Active = true,
            ActiveComparison = ActiveComparison.Equal,
            Name = "item",
            NameComparison = NameComparison.Contains,
            Value = 100,
            ValueComparison = ValueComparison.GreaterThanOrEqual
        };

        ActionResult<FilteredRecordsResponse> result = await new FilesController(store)
            .Records("test-key", "file-1", filter, CancellationToken.None);

        FilteredRecordsResponse response = Assert.IsType<FilteredRecordsResponse>(result.Value);
        Assert.Equal(10, response.TotalCount);
        Assert.Equal([3, 7, 10], response.Records.Select(record => record.Id));
        Assert.Equal(3, response.MatchedCount);
    }

    [Fact]
    public async Task Records_ActiveNotEqual_ReturnsInactiveRecords()
    {
        InMemoryFileStore store = new() { RetrievedFile = CreateStoredFile() };
        RecordFilterQuery filter = new()
        {
            Active = true,
            ActiveComparison = ActiveComparison.NotEqual
        };

        ActionResult<FilteredRecordsResponse> result = await new FilesController(store)
            .Records("test-key", "file-1", filter, CancellationToken.None);

        FilteredRecordsResponse response = Assert.IsType<FilteredRecordsResponse>(result.Value);
        Assert.Equal([2, 4, 6, 8], response.Records.Select(record => record.Id));
    }

    [Theory]
    [InlineData(NameComparison.Equal, "Premium Item", 5)]
    [InlineData(NameComparison.StartsWith, "North", 7)]
    public async Task Records_AppliesNameComparison(
        NameComparison comparison,
        string name,
        int expectedId)
    {
        InMemoryFileStore store = new() { RetrievedFile = CreateStoredFile() };
        RecordFilterQuery filter = new()
        {
            Name = name,
            NameComparison = comparison
        };

        ActionResult<FilteredRecordsResponse> result = await new FilesController(store)
            .Records("test-key", "file-1", filter, CancellationToken.None);

        FilteredRecordsResponse response = Assert.IsType<FilteredRecordsResponse>(result.Value);
        Assert.Single(response.Records);
        Assert.Equal(expectedId, response.Records[0].Id);
    }

    [Theory]
    [InlineData(ValueComparison.Equal, 50, "4,5")]
    [InlineData(ValueComparison.GreaterThan, 100, "3,7,10")]
    [InlineData(ValueComparison.GreaterThanOrEqual, 100, "3,7,10")]
    [InlineData(ValueComparison.LessThan, 50, "2,6,9")]
    [InlineData(ValueComparison.LessThanOrEqual, 50, "2,4,5,6,9")]
    public async Task Records_AppliesValueComparison(
        ValueComparison comparison,
        double value,
        string expectedIds)
    {
        InMemoryFileStore store = new() { RetrievedFile = CreateStoredFile() };
        RecordFilterQuery filter = new()
        {
            Value = value,
            ValueComparison = comparison
        };

        ActionResult<FilteredRecordsResponse> result = await new FilesController(store)
            .Records("test-key", "file-1", filter, CancellationToken.None);

        FilteredRecordsResponse response = Assert.IsType<FilteredRecordsResponse>(result.Value);
        int[] ids = expectedIds.Split(',').Select(int.Parse).ToArray();
        Assert.Equal(ids, response.Records.Select(record => record.Id));
    }

    [Fact]
    public async Task Records_WhenFileDoesNotExist_ReturnsNotFound()
    {
        ActionResult<FilteredRecordsResponse> result = await new FilesController(
            new InMemoryFileStore()).Records(
                "test-key",
                "missing-file",
                new(),
                CancellationToken.None);

        NotFoundObjectResult notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("File not found.", notFound.Value);
    }

    [Fact]
    public async Task Records_WhenComparisonHasNoFilterValue_ReturnsBadRequest()
    {
        InMemoryFileStore store = new() { RetrievedFile = CreateStoredFile() };
        RecordFilterQuery filter = new() { ValueComparison = ValueComparison.GreaterThan };

        ActionResult<FilteredRecordsResponse> result = await new FilesController(store)
            .Records("test-key", "file-1", filter, CancellationToken.None);

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("valueComparison requires value.", badRequest.Value);
    }

    private static StoredFile CreateStoredFile() => new()
    {
        Id = "file-1",
        FileName = "items.json",
        Size = 844,
        UploadedAtUtc = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc),
        Records =
        [
            new(1, "Item A", true, 75),
            new(2, "Item B", false, 25),
            new(3, "Item C", true, 120),
            new(4, "Item D", false, 50),
            new(5, "Premium Item", true, 50),
            new(6, "Archived Item", false, 10),
            new(7, "North Item", true, 200),
            new(8, "South Item", false, 90),
            new(9, "Zero Value Item", true, 0),
            new(10, "Final Item", true, 150)
        ]
    };

    private sealed class InMemoryFileStore : IFileStore
    {
        public StoredFile? SavedFile { get; set; }

        public StoredFile? RetrievedFile { get; set; }

        public IReadOnlyList<FileReportItem> Report { get; set; } = [];

        public Task SaveAsync(StoredFile file)
        {
            SavedFile = file;
            return Task.CompletedTask;
        }

        public Task<StoredFile?> GetAsync(
            string id,
            CancellationToken cancellationToken = default) => Task.FromResult(RetrievedFile);

        public Task<IReadOnlyList<FileReportItem>> GetReportAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Report);
        }
    }
}

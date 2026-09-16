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

    private sealed class InMemoryFileStore : IFileStore
    {
        public StoredFile? SavedFile { get; set; }

        public Task SaveAsync(StoredFile file)
        {
            SavedFile = file;
            return Task.CompletedTask;
        }

    }
}

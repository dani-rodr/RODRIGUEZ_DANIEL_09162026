using FileProcessing.Api.Controllers;
using FileProcessing.Api.Models;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace FileProcessing.Api.Tests;

public sealed class FilesControllerTests
{
    [Fact]
    public void Process_ReturnsUploadedFileMetadata()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("test content"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "items.json");

        var response = new FilesController().Process("test-key", file);

        Assert.Equal(new UploadResponse("items.json", stream.Length), response);
    }
}

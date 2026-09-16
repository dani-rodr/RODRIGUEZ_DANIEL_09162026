using FileProcessing.Api.Controllers;
using FileProcessing.Api.Models;

namespace FileProcessing.Api.Tests;

public sealed class FilesControllerTests
{
    [Fact]
    public void Process_ReturnsSuccess()
    {
        var response = new FilesController().Process("test-key");

        Assert.Equal(new ProcessResponse(true), response);
    }
}

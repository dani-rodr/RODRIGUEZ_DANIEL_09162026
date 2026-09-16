using FileProcessing.Api.Controllers;

namespace FileProcessing.Api.Tests;

public sealed class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsHealthyStatus()
    {
        var response = new HealthController().Get();

        Assert.Equal("ok", response.Status);
    }
}

using FileProcessing.Api.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FileProcessing.Api.Tests;

public sealed class ApiKeyMiddlewareTests
{
    [Fact]
    public async Task UploadRequest_WithCorrectKey_CallsNext()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/api/files/upload", "test-key", "test-key");

        Assert.Equal(StatusCodes.Status200OK, StatusCode);
        Assert.True(NextCalled);
    }

    [Fact]
    public async Task UploadRequest_WithoutKey_ReturnsUnauthorized()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/api/files/upload", null, "test-key");

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusCode);
        Assert.False(NextCalled);
    }

    [Fact]
    public async Task UploadRequest_WithIncorrectKey_ReturnsUnauthorized()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/api/files/upload", "wrong-key", "test-key");

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusCode);
        Assert.False(NextCalled);
    }

    [Fact]
    public async Task UploadRequest_WithoutConfiguredKey_ReturnsUnauthorized()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/api/files/upload", "test-key", null);

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusCode);
        Assert.False(NextCalled);
    }

    [Fact]
    public async Task HealthRequest_DoesNotRequireKey()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/health", null, "test-key");

        Assert.Equal(StatusCodes.Status200OK, StatusCode);
        Assert.True(NextCalled);
    }

    private static async Task<(int StatusCode, bool NextCalled)> InvokeAsync(
        string path,
        string? suppliedKey,
        string? configuredKey)
    {
        ConfigurationManager configuration = new();
        configuration["ApiKey:Value"] = configuredKey;
        NextTracker tracker = new();
        ApiKeyMiddleware middleware = new(tracker.InvokeAsync, configuration);

        return await InvokeAsync(middleware, tracker, suppliedKey, path);
    }

    private static async Task<(int StatusCode, bool NextCalled)> InvokeAsync(
        ApiKeyMiddleware middleware,
        NextTracker tracker,
        string? suppliedKey,
        string path = "/api/files/upload")
    {
        tracker.Reset();
        DefaultHttpContext context = new();
        context.Request.Path = path;
        if (suppliedKey is not null)
        {
            context.Request.Headers[ApiKeyMiddleware.HeaderName] = suppliedKey;
        }

        await middleware.InvokeAsync(context);

        return (context.Response.StatusCode, tracker.Called);
    }

    private sealed class NextTracker
    {
        public bool Called { get; private set; }

        public Task InvokeAsync(HttpContext context)
        {
            Called = true;
            return Task.CompletedTask;
        }

        public void Reset() => Called = false;
    }
}

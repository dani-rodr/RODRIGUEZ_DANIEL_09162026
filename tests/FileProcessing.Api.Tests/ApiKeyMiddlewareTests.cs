using FileProcessing.Api.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FileProcessing.Api.Tests;

public sealed class ApiKeyMiddlewareTests
{
    [Fact]
    public async Task ProcessRequest_WithCorrectKey_CallsNext()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/api/files/process", "test-key", "test-key");

        Assert.Equal(StatusCodes.Status200OK, StatusCode);
        Assert.True(NextCalled);
    }

    [Fact]
    public async Task ProcessRequest_WithoutKey_ReturnsUnauthorized()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/api/files/process", null, "test-key");

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusCode);
        Assert.False(NextCalled);
    }

    [Fact]
    public async Task ProcessRequest_WithIncorrectKey_ReturnsUnauthorized()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/api/files/process", "wrong-key", "test-key");

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusCode);
        Assert.False(NextCalled);
    }

    [Fact]
    public async Task ProcessRequest_WithoutConfiguredKey_ReturnsUnauthorized()
    {
        var (StatusCode, NextCalled) = await InvokeAsync("/api/files/process", "test-key", null);

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
        string path = "/api/files/process")
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

using System.Security.Cryptography;
using System.Text;

namespace FileProcessing.Api.Authentication;

public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public const string HeaderName = "X-API-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/files"))
        {
            string? suppliedKey = context.Request.Headers[HeaderName].FirstOrDefault();
            string? configuredKey = configuration["ApiKey:Value"];

            if (string.IsNullOrEmpty(suppliedKey) ||
                string.IsNullOrEmpty(configuredKey) ||
                !KeysMatch(suppliedKey, configuredKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }

    private static bool KeysMatch(string suppliedKey, string configuredKey) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(suppliedKey),
            Encoding.UTF8.GetBytes(configuredKey));
}

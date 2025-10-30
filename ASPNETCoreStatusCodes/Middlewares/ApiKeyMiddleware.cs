namespace ASPNETCoreStatusCodes.Middlewares;
// ReSharper disable All

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["ApiKey"] ?? "demo-api-key-12345";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key validation for health checks or documentation endpoints
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            context.Response.StatusCode = 401; // 401 Unauthorized
            await context.Response.WriteAsync("API Key missing");
            return;
        }

        if (!_apiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }

        // The same applies when using JWT Bearer authentication middleware.
        // In ASP.NET Core, if a JWT token is invalid, the server typically returns a 401 Unauthorized HTTP status code.

        await _next(context);
    }
}
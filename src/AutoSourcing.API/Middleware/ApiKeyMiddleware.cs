namespace AutoSourcing.API.Middleware;

public class ApiKeyMiddleware
{
    public const string HeaderName = "X-API-Key";

    private static readonly string[] PublicPrefixes =
    [
        "/api/consent",
        "/api/unsubscribe",
        "/api/tracking",
        "/api/agent",
        "/swagger"
    ];

    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["ApiKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        var isApi = path.StartsWith("/api", StringComparison.OrdinalIgnoreCase);
        var isPublic = PublicPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (isApi && !isPublic && !string.IsNullOrWhiteSpace(_apiKey))
        {
            if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
                !string.Equals(provided.ToString(), _apiKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized. Provide a valid X-API-Key header." });
                return;
            }
        }

        await _next(context);
    }
}

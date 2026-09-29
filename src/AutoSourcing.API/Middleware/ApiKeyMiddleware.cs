using AutoSourcing.API.Auth;
using AutoSourcing.Services.Auth;

namespace AutoSourcing.API.Middleware;

public class ApiKeyMiddleware
{
    public const string HeaderName = "X-API-Key";

    private static readonly string[] PublicPrefixes =
    [
        "/api/auth/login",
        "/api/auth/setup",
        "/api/auth/status",
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

        if (!isApi || isPublic)
        {
            await _next(context);
            return;
        }

        // 1. Signed-in user (bearer session token). Populates the current user for the request.
        var token = AuthConstants.ExtractBearerToken(context.Request);
        if (!string.IsNullOrWhiteSpace(token))
        {
            var authService = context.RequestServices.GetRequiredService<IAuthService>();
            var user = await authService.ResolveAsync(token, context.RequestAborted);
            if (user is not null)
            {
                context.Items[AuthConstants.CurrentUserItem] = user;
                await _next(context);
                return;
            }
        }

        // 2. Server-to-server API key (e.g. the Scotty MCP endpoint).
        if (!string.IsNullOrWhiteSpace(_apiKey) &&
            context.Request.Headers.TryGetValue(HeaderName, out var provided) &&
            string.Equals(provided.ToString(), _apiKey, StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized. Sign in or provide a valid X-API-Key header." });
    }
}

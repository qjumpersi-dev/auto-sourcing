namespace AutoSourcing.API.Auth;

public static class AuthConstants
{
    public const string CurrentUserItem = "CurrentUser";
    public const string BearerPrefix = "Bearer ";

    public static string? ExtractBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[BearerPrefix.Length..].Trim()
            : null;
    }
}

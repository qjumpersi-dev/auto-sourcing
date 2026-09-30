namespace AutoSourcing.Services.Microsoft;

public class MicrosoftOptions
{
    public const string SectionName = "Microsoft";

    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = "common";
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;

    // Where to send the browser back to after the Microsoft consent screen.
    public string AppBaseUrl { get; set; } = "http://localhost:5173";

    public string Authority => $"https://login.microsoftonline.com/{TenantId}";

    public static readonly string[] Scopes =
    [
        "offline_access",
        "openid",
        "profile",
        "email",
        "User.Read",
        "Mail.Send"
    ];
}

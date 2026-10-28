namespace AutoSourcing.Services.LinkedIn;

public class LinkedInOptions
{
    public const string SectionName = "LinkedIn";

    public bool Headless { get; set; } = false;
    public string UserDataDir { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoSourcing", "LinkedInProfile");
    public int ActionTimeoutMs { get; set; } = 30000;
    public bool DryRun { get; set; } = false;
    public string? BrowserExecutablePath { get; set; }

    // "Server": send LinkedIn inline (requires a browser on the API host).
    // "Local": queue LinkedIn messages for a local worker to send from a machine with a browser.
    public string Mode { get; set; } = "Server";

    public bool IsLocalMode => string.Equals(Mode, "Local", StringComparison.OrdinalIgnoreCase);
}
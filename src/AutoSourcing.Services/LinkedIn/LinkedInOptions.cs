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
}
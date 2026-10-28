namespace AutoSourcing.Services.Rhetorik;

public class RhetorikOptions
{
    public const string SectionName = "Rhetorik";

    public string BaseUrl { get; set; } = "https://api.rhetorik360.io/";
    public string ApiKey { get; set; } = string.Empty;

    // Revealing contact emails (Rhetorik "reveal_all_data") costs money per profile.
    // Off by default; turn on only when you intend to pay for email discovery.
    public bool RevealContactEmails { get; set; } = false;
}

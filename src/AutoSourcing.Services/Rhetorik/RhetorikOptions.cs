namespace AutoSourcing.Services.Rhetorik;

public class RhetorikOptions
{
    public const string SectionName = "Rhetorik";

    public string BaseUrl { get; set; } = "https://api.rhetorik360.io/";
    public string ApiKey { get; set; } = string.Empty;
}

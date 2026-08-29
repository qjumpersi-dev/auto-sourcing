namespace AutoSourcing.Services.Scotty;

public class ScottyOptions
{
    public const string SectionName = "Scotty";

    public string BaseUrl { get; set; } = "https://api.scotty-ai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string RestChannelId { get; set; } = string.Empty;
    public string WebRtcChannelId { get; set; } = string.Empty;
}
namespace AutoSourcing.Services.Scotty;

public class ScottyOptions
{
    public const string SectionName = "Scotty";

    public string BaseUrl { get; set; } = "https://api.scotty-ai.com/v1";
    public string ApiKey { get; set; } = string.Empty;

    // Recruiter-facing assistant.
    public string RestChannelId { get; set; } = string.Empty;
    public string WebRtcChannelId { get; set; } = string.Empty;

    // Candidate-facing agent (answers candidates' questions about the role/company).
    // Falls back to the recruiter channels when not configured.
    public string CandidateRestChannelId { get; set; } = string.Empty;
    public string CandidateWebRtcChannelId { get; set; } = string.Empty;
}
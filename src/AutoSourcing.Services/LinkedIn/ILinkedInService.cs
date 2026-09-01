namespace AutoSourcing.Services.LinkedIn;

public record LinkedInPageInfo(string Url, string Title, bool NavPrimaryItems, bool NavGlobal, string BodyTextSample);

public class LinkedInSendResult
{
    public bool Sent { get; init; }
    public string? Message { get; init; }
}

public interface ILinkedInService
{
    Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default);
    Task<bool> SignInAsync(CancellationToken cancellationToken = default);
    Task<LinkedInSendResult> SendInMailAsync(string profileUrl, string subject, string body, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LinkedInPageInfo>> GetOpenPagesAsync(CancellationToken cancellationToken = default);
    Task<object?> ProbeDomAsync(string url, CancellationToken cancellationToken = default);
}
namespace AutoSourcing.Services.Scotty;

public interface IScottyClient
{
    Task<ScottyChatResponse> SendTextAsync(ScottyChatRequest request, CancellationToken cancellationToken = default);
    Task<ScottyCallResponse> GetCallCredentialAsync(ScottyCallRequest request, CancellationToken cancellationToken = default);
}
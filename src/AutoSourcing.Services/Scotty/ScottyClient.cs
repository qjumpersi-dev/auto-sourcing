using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Scotty;

public class ScottyClient : IScottyClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ScottyOptions _options;

    public ScottyClient(HttpClient httpClient, IOptions<ScottyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress ??= new Uri(_options.BaseUrl);
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("API-KEY");
            _httpClient.DefaultRequestHeaders.Add("API-KEY", _options.ApiKey);
        }
    }

    public async Task<ScottyChatResponse> SendTextAsync(ScottyChatRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            user_prompt = request.UserPrompt,
            continuity_key = request.ContinuityKey
        };

        var uri = new Uri($"{_options.BaseUrl.TrimEnd('/')}/channels/rest/{_options.RestChannelId}");
        using var response = await _httpClient.PostAsJsonAsync(uri, payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Scotty chat failed with {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<ScottyChatResponse>(JsonOptions, cancellationToken)
            ?? new ScottyChatResponse();
    }

    public async Task<ScottyCallResponse> GetCallCredentialAsync(ScottyCallRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            session_participant_id = request.SessionParticipantId,
            continuity_key = request.ContinuityKey
        };

        var uri = new Uri($"{_options.BaseUrl.TrimEnd('/')}/channels/webrtc/{_options.WebRtcChannelId}");
        using var response = await _httpClient.PostAsJsonAsync(uri, payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Scotty call credential failed with {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<ScottyCallResponse>(JsonOptions, cancellationToken)
            ?? new ScottyCallResponse();
    }
}
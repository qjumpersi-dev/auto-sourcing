using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Microsoft;

public record MicrosoftTokens(string AccessToken, string? RefreshToken, DateTime ExpiresAt);

public record MicrosoftAccount(string Email, string? DisplayName);

public record MicrosoftSendMail(string To, string Subject, string HtmlBody, string? ReplyTo, string? FromAddress, string? FromName);

public interface IMicrosoftGraphService
{
    string BuildAuthorizeUrl(string state);
    Task<MicrosoftTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<MicrosoftTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<MicrosoftAccount> GetAccountAsync(string accessToken, CancellationToken cancellationToken = default);
    Task SendMailAsync(string accessToken, MicrosoftSendMail message, CancellationToken cancellationToken = default);
}

public class MicrosoftGraphService : IMicrosoftGraphService
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";

    private readonly HttpClient _httpClient;
    private readonly MicrosoftOptions _options;

    public MicrosoftGraphService(HttpClient httpClient, IOptions<MicrosoftOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string BuildAuthorizeUrl(string state)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _options.RedirectUri,
            ["response_mode"] = "query",
            ["scope"] = string.Join(' ', MicrosoftOptions.Scopes),
            ["state"] = state,
            ["prompt"] = "select_account"
        };

        var queryString = string.Join('&', query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        return $"{_options.Authority}/oauth2/v2.0/authorize?{queryString}";
    }

    public Task<MicrosoftTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
        => PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
            ["scope"] = string.Join(' ', MicrosoftOptions.Scopes)
        }, cancellationToken);

    public Task<MicrosoftTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        => PostTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["scope"] = string.Join(' ', MicrosoftOptions.Scopes)
        }, cancellationToken);

    public async Task<MicrosoftAccount> GetAccountAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{GraphBase}/me?$select=mail,userPrincipalName,displayName");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Microsoft Graph /me failed ({(int)response.StatusCode}): {body}");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var email = root.TryGetProperty("mail", out var mail) && mail.ValueKind == JsonValueKind.String
            ? mail.GetString()
            : null;
        email ??= root.TryGetProperty("userPrincipalName", out var upn) && upn.ValueKind == JsonValueKind.String
            ? upn.GetString()
            : null;
        var displayName = root.TryGetProperty("displayName", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;

        return new MicrosoftAccount(email ?? string.Empty, displayName);
    }

    public async Task SendMailAsync(string accessToken, MicrosoftSendMail message, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["message"] = new Dictionary<string, object?>
            {
                ["subject"] = message.Subject,
                ["body"] = new Dictionary<string, object?>
                {
                    ["contentType"] = "HTML",
                    ["content"] = message.HtmlBody
                },
                ["toRecipients"] = new object[] { Recipient(message.To) }
            },
            ["saveToSentItems"] = true
        };

        var graphMessage = (Dictionary<string, object?>)payload["message"]!;

        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
        {
            graphMessage["replyTo"] = new object[] { Recipient(message.ReplyTo) };
        }

        if (!string.IsNullOrWhiteSpace(message.FromAddress))
        {
            var emailAddress = new Dictionary<string, object?> { ["address"] = message.FromAddress };
            if (!string.IsNullOrWhiteSpace(message.FromName))
            {
                emailAddress["name"] = message.FromName;
            }

            graphMessage["from"] = new Dictionary<string, object?> { ["emailAddress"] = emailAddress };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{GraphBase}/me/sendMail")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Microsoft Graph sendMail failed ({(int)response.StatusCode}): {body}");
        }
    }

    private static Dictionary<string, object?> Recipient(string address) =>
        new() { ["emailAddress"] = new Dictionary<string, object?> { ["address"] = address } };

    private async Task<MicrosoftTokens> PostTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(
            $"{_options.Authority}/oauth2/v2.0/token",
            new FormUrlEncodedContent(form),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Microsoft token request failed ({(int)response.StatusCode}): {body}");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var accessToken = root.GetProperty("access_token").GetString() ?? string.Empty;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) && rt.ValueKind == JsonValueKind.String
            ? rt.GetString()
            : null;

        var expiresIn = 3600;
        if (root.TryGetProperty("expires_in", out var expiresElement) && expiresElement.TryGetInt32(out var seconds))
        {
            expiresIn = seconds;
        }

        return new MicrosoftTokens(accessToken, refreshToken, DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60)));
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Microsoft;

public record MicrosoftTokens(string AccessToken, string? RefreshToken, DateTime ExpiresAt);

public record MicrosoftAccount(string Email, string? DisplayName);

public record MicrosoftSendMail(string To, string Subject, string HtmlBody, string? ReplyTo, string? FromAddress, string? FromName);

public record BusySlot(DateTime StartUtc, DateTime EndUtc);

public record MeetingAttendee(string Email, string? Name);

public record TeamsMeeting(string EventId, string? JoinUrl);

public interface IMicrosoftGraphService
{
    string BuildAuthorizeUrl(string state);
    Task<MicrosoftTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<MicrosoftTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<MicrosoftAccount> GetAccountAsync(string accessToken, CancellationToken cancellationToken = default);
    Task SendMailAsync(string accessToken, MicrosoftSendMail message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusySlot>> GetBusySlotsAsync(string accessToken, string mailbox, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<TeamsMeeting> CreateOnlineMeetingAsync(string accessToken, string subject, string body, DateTime startUtc, DateTime endUtc, IEnumerable<MeetingAttendee> attendees, CancellationToken cancellationToken = default);
    Task RescheduleMeetingAsync(string accessToken, string eventId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task CancelMeetingAsync(string accessToken, string eventId, string? comment, CancellationToken cancellationToken = default);
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

    public async Task<IReadOnlyList<BusySlot>> GetBusySlotsAsync(string accessToken, string mailbox, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["schedules"] = new[] { mailbox },
            ["startTime"] = GraphDateTime(startUtc),
            ["endTime"] = GraphDateTime(endUtc),
            ["availabilityViewInterval"] = 30
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{GraphBase}/me/calendar/getSchedule")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Microsoft Graph getSchedule failed ({(int)response.StatusCode}): {body}");
        }

        var slots = new List<BusySlot>();
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return slots;
        }

        foreach (var schedule in value.EnumerateArray())
        {
            if (!schedule.TryGetProperty("scheduleItems", out var items) || items.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in items.EnumerateArray())
            {
                var start = ReadGraphDate(item, "start");
                var end = ReadGraphDate(item, "end");
                if (start is not null && end is not null)
                {
                    slots.Add(new BusySlot(start.Value, end.Value));
                }
            }
        }

        return slots;
    }

    public async Task<TeamsMeeting> CreateOnlineMeetingAsync(string accessToken, string subject, string body, DateTime startUtc, DateTime endUtc, IEnumerable<MeetingAttendee> attendees, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["subject"] = subject,
            ["body"] = new Dictionary<string, object?> { ["contentType"] = "HTML", ["content"] = body },
            ["start"] = GraphDateTime(startUtc),
            ["end"] = GraphDateTime(endUtc),
            ["isOnlineMeeting"] = true,
            ["onlineMeetingProvider"] = "teamsForBusiness",
            ["attendees"] = attendees.Select(a => new Dictionary<string, object?>
            {
                ["emailAddress"] = new Dictionary<string, object?> { ["address"] = a.Email, ["name"] = a.Name ?? a.Email },
                ["type"] = "required"
            }).ToArray()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{GraphBase}/me/events")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Microsoft Graph create event failed ({(int)response.StatusCode}): {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        var eventId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrEmpty(eventId))
        {
            throw new InvalidOperationException("Microsoft Graph did not return an event id.");
        }

        string? joinUrl = null;
        if (root.TryGetProperty("onlineMeeting", out var online) && online.ValueKind == JsonValueKind.Object &&
            online.TryGetProperty("joinUrl", out var join))
        {
            joinUrl = join.GetString();
        }

        return new TeamsMeeting(eventId, joinUrl);
    }

    public async Task RescheduleMeetingAsync(string accessToken, string eventId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["start"] = GraphDateTime(startUtc),
            ["end"] = GraphDateTime(endUtc)
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{GraphBase}/me/events/{Uri.EscapeDataString(eventId)}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Microsoft Graph reschedule failed ({(int)response.StatusCode}): {body}");
        }
    }

    public async Task CancelMeetingAsync(string accessToken, string eventId, string? comment, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{GraphBase}/me/events/{Uri.EscapeDataString(eventId)}/cancel")
        {
            Content = JsonContent.Create(new Dictionary<string, object?>
            {
                ["comment"] = string.IsNullOrWhiteSpace(comment) ? "This interview has been cancelled." : comment
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Microsoft Graph cancel failed ({(int)response.StatusCode}): {body}");
        }
    }

    private static Dictionary<string, object?> GraphDateTime(DateTime utc) => new()
    {
        ["dateTime"] = utc.ToString("yyyy-MM-ddTHH:mm:ss"),
        ["timeZone"] = "UTC"
    };

    private static DateTime? ReadGraphDate(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty("dateTime", out var dateTime) || dateTime.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTime.TryParse(
            dateTime.GetString(),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : null;
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

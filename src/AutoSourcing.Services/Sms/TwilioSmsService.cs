using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Sms;

public class TwilioSmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(HttpClient httpClient, IOptions<SmsOptions> options, ILogger<TwilioSmsService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.TwilioAccountSid) || string.IsNullOrWhiteSpace(_options.TwilioAuthToken))
        {
            return new SmsSendResult(false, "None", "Twilio credentials not configured.");
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            return new SmsSendResult(false, "None", "Recipient phone number is missing.");
        }

        try
        {
            await SendViaTwilioAsync(to, message, "sms", cancellationToken);
            return new SmsSendResult(true, "SMS", null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMS send failed for {To}, attempting RCS fallback.", to);

            if (_options.UseRcsFallback)
            {
                try
                {
                    await SendViaTwilioAsync(to, message, "rcs", cancellationToken);
                    return new SmsSendResult(true, "RCS", null);
                }
                catch (Exception rcsEx)
                {
                    _logger.LogError(rcsEx, "RCS fallback also failed for {To}.", to);
                    return new SmsSendResult(false, "None", rcsEx.Message);
                }
            }

            return new SmsSendResult(false, "None", ex.Message);
        }
    }

    private async Task SendViaTwilioAsync(string to, string message, string channelType, CancellationToken cancellationToken)
    {
        var accountSid = _options.TwilioAccountSid!;
        var authToken = _options.TwilioAuthToken!;
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

        var form = new Dictionary<string, string>
        {
            ["To"] = to,
            ["From"] = _options.FromNumber ?? string.Empty,
            ["Body"] = message
        };

        if (channelType == "rcs")
        {
            form["Media"] = ""; // RCS may need different parameters
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(form);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Twilio {channelType} send failed ({(int)response.StatusCode}): {errorBody}");
        }
    }
}

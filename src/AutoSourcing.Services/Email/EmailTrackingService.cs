using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Email;

public interface IEmailTrackingService
{
    string RewriteLinks(string html, int messageId);
    string RewriteLinksInPlainText(string text, int messageId);
    string BuildOpenPixel(int messageId);
}

public partial class EmailTrackingService : IEmailTrackingService
{
    private readonly EmailOptions _options;

    public EmailTrackingService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public string RewriteLinks(string html, int messageId)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        var baseUrl = BaseUrl;
        return HrefPattern().Replace(html, match =>
        {
            var url = match.Groups["url"].Value;
            if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("/api/tracking/", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("/api/unsubscribe", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("/api/consent", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            var tracked = $"{baseUrl}/api/tracking/click/{messageId}/{UrlToken.Encode(url)}";
            return $"href=\"{tracked}\"";
        });
    }

    public string RewriteLinksInPlainText(string text, int messageId)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var baseUrl = BaseUrl;
        return PlainUrlPattern().Replace(text, match =>
        {
            var value = match.Value;
            var trailing = string.Empty;
            while (value.Length > 0 && TrailingPunctuation.Contains(value[^1]))
            {
                trailing = value[^1] + trailing;
                value = value[..^1];
            }

            if (value.Length == 0 ||
                value.Contains("/api/tracking/", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("/api/unsubscribe", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("/api/consent", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            return $"{baseUrl}/api/tracking/click/{messageId}/{UrlToken.Encode(value)}{trailing}";
        });
    }

    public string BuildOpenPixel(int messageId)
    {
        return $"<img src=\"{BaseUrl}/api/tracking/open/{messageId}.gif\" width=\"1\" height=\"1\" alt=\"\" style=\"display:block;width:1px;height:1px;\" />";
    }

    private string BaseUrl => (_options.PublicBaseUrl ?? string.Empty).TrimEnd('/');

    private const string TrailingPunctuation = ".,;:!?)]}";

    [GeneratedRegex("https?://[^\\s<>\"']+", RegexOptions.IgnoreCase)]
    private static partial Regex PlainUrlPattern();

    [GeneratedRegex("href=\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex HrefPattern();
}

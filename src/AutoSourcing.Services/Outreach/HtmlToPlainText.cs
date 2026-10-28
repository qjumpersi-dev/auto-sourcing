using System.Net;
using System.Text.RegularExpressions;

namespace AutoSourcing.Services.Outreach;

public static partial class HtmlToPlainText
{
    public static string Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        var text = LinkPattern().Replace(html, match =>
        {
            var url = match.Groups["url"].Value;
            var label = StripTags(match.Groups["label"].Value).Trim();
            return string.IsNullOrWhiteSpace(label) || label == url ? url : $"{label} ({url})";
        });

        text = BlockTagPattern().Replace(text, "\n");
        text = BreakTagPattern().Replace(text, "\n");
        text = TagPattern().Replace(text, string.Empty);

        text = WebUtility.HtmlDecode(text).Replace('\u00A0', ' ');

        return string.Join("\n", text
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0));
    }

    private static string StripTags(string html)
    {
        return TagPattern().Replace(html, string.Empty);
    }

    [GeneratedRegex(@"<a\b[^>]*href=""(?<url>[^""]+)""[^>]*>(?<label>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"</?(?:div|p|li|ul|ol|tr|table|h[1-6])(?:\s[^>]*)?>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockTagPattern();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BreakTagPattern();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex TagPattern();
}
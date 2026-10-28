namespace AutoSourcing.Services.Rhetorik;

public static class RhetorikSocialLinks
{
    private const int MaxUrlLength = 500;

    // Pulls the LinkedIn profile URL out of the profile's social links, normalising it to https.
    public static string? ExtractLinkedInUrl(IReadOnlyList<RhetorikSocialLink>? links)
    {
        if (links is null || links.Count == 0)
        {
            return null;
        }

        var link = links.FirstOrDefault(l =>
            (!string.IsNullOrWhiteSpace(l.Name) && l.Name.Contains("linkedin", StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(l.Url) && l.Url.Contains("linkedin", StringComparison.OrdinalIgnoreCase)));

        var url = link?.Url?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url.TrimStart('/');
        }

        return url.Length <= MaxUrlLength ? url : url[..MaxUrlLength];
    }
}

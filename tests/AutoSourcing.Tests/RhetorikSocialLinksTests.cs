using AutoSourcing.Services.Rhetorik;
using Xunit;

namespace AutoSourcing.Tests;

public class RhetorikSocialLinksTests
{
    [Fact]
    public void ExtractLinkedInUrl_FindsLinkedInByName_AndAddsScheme()
    {
        var links = new[] { new RhetorikSocialLink { Name = "LinkedIn", Url = "linkedin.com/in/jane-doe" } };

        Assert.Equal("https://linkedin.com/in/jane-doe", RhetorikSocialLinks.ExtractLinkedInUrl(links));
    }

    [Fact]
    public void ExtractLinkedInUrl_KeepsExistingScheme()
    {
        var links = new[] { new RhetorikSocialLink { Name = "LinkedIn", Url = "https://www.linkedin.com/in/jane-doe" } };

        Assert.Equal("https://www.linkedin.com/in/jane-doe", RhetorikSocialLinks.ExtractLinkedInUrl(links));
    }

    [Fact]
    public void ExtractLinkedInUrl_MatchesByUrl_WhenNameMissing()
    {
        var links = new[] { new RhetorikSocialLink { Url = "linkedin.com/in/jane-doe" } };

        Assert.Equal("https://linkedin.com/in/jane-doe", RhetorikSocialLinks.ExtractLinkedInUrl(links));
    }

    [Fact]
    public void ExtractLinkedInUrl_IgnoresOtherNetworks()
    {
        var links = new[]
        {
            new RhetorikSocialLink { Name = "Twitter", Url = "twitter.com/jane" },
            new RhetorikSocialLink { Name = "LinkedIn", Url = "linkedin.com/in/jane-doe" }
        };

        Assert.Equal("https://linkedin.com/in/jane-doe", RhetorikSocialLinks.ExtractLinkedInUrl(links));
    }

    [Fact]
    public void ExtractLinkedInUrl_ReturnsNull_WhenNoLinkedIn()
    {
        var links = new[] { new RhetorikSocialLink { Name = "Twitter", Url = "twitter.com/jane" } };

        Assert.Null(RhetorikSocialLinks.ExtractLinkedInUrl(links));
    }

    [Fact]
    public void ExtractLinkedInUrl_ReturnsNull_WhenEmpty()
    {
        Assert.Null(RhetorikSocialLinks.ExtractLinkedInUrl(null));
        Assert.Null(RhetorikSocialLinks.ExtractLinkedInUrl(Array.Empty<RhetorikSocialLink>()));
    }
}

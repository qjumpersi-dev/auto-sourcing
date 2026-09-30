using AutoSourcing.Services.Email;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoSourcing.Tests;

public class EmailTrackingServiceTests
{
    private static EmailTrackingService CreateSut() =>
        new(Options.Create(new EmailOptions { PublicBaseUrl = "https://app.test" }));

    [Fact]
    public void RewriteLinks_WrapsHrefInTrackingUrl()
    {
        var sut = CreateSut();

        var result = sut.RewriteLinks("<a href=\"https://example.com/page?a=1&b=2\">link</a>", 42);

        Assert.Contains("href=\"https://app.test/api/tracking/go/42/", result);
        Assert.DoesNotContain("?url=", result);
    }

    [Fact]
    public void RewriteLinks_LeavesUnsubscribeAndMailtoUntouched()
    {
        var sut = CreateSut();

        var result = sut.RewriteLinks(
            "<a href=\"https://app.test/api/unsubscribe/7\">Unsubscribe</a><a href=\"mailto:hi@example.com\">Mail</a>",
            42);

        Assert.Contains("href=\"https://app.test/api/unsubscribe/7\"", result);
        Assert.Contains("href=\"mailto:hi@example.com\"", result);
        Assert.DoesNotContain("/api/tracking/go/42", result);
    }

    [Fact]
    public void BuildOpenPixel_PointsAtTrackingEndpoint()
    {
        var sut = CreateSut();

        var result = sut.BuildOpenPixel(42);

        Assert.Contains("https://app.test/api/tracking/open/42.gif", result);
    }

    [Fact]
    public void RewriteLinksInPlainText_WrapsUrlInTrackingUrl()
    {
        var sut = CreateSut();

        var result = sut.RewriteLinksInPlainText("Click the link https://example.com/job/9", 42);

        Assert.Contains("https://app.test/api/tracking/go/42/", result);
        Assert.DoesNotContain("?url=", result);
    }

    [Fact]
    public void RewriteLinksInPlainText_LeavesTrackingAndUnsubscribeUrlsAlone()
    {
        var sut = CreateSut();

        var result = sut.RewriteLinksInPlainText(
            "A https://app.test/api/tracking/click/7/abc and https://app.test/api/unsubscribe/7",
            42);

        Assert.DoesNotContain("/api/tracking/go/42/", result);
    }

    [Fact]
    public void RewriteLinksInPlainText_KeepsTrailingPunctuation()
    {
        var sut = CreateSut();

        var result = sut.RewriteLinksInPlainText("See https://example.com/job.", 42);

        Assert.EndsWith($"/api/tracking/go/42/{UrlToken.Encode("https://example.com/job")}.", result);
    }
}

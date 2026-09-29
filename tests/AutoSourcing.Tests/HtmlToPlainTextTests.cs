using AutoSourcing.Services.Outreach;
using Xunit;

namespace AutoSourcing.Tests;

public class HtmlToPlainTextTests
{
    [Fact]
    public void Convert_StripsTagsAndDecodesEntities()
    {
        var result = HtmlToPlainText.Convert("Hi <b>Annette</b><div>Line 1</div><div><br></div><div>Line 2 &amp; done</div>");

        Assert.Equal("Hi Annette\nLine 1\nLine 2 & done", result);
    }

    [Fact]
    public void Convert_ExtractsLinkUrl_WhenLabelIsUrl()
    {
        var result = HtmlToPlainText.Convert(
            "<div>Click the link</div><div><a href=\"https://example.com/job/1\">https://example.com/job/1</a></div>");

        Assert.Equal("Click the link\nhttps://example.com/job/1", result);
    }

    [Fact]
    public void Convert_InlinesLinkUrl_WhenLabelDiffers()
    {
        var result = HtmlToPlainText.Convert("<a href=\"https://example.com/job/2\">View the job</a>");

        Assert.Equal("View the job (https://example.com/job/2)", result);
    }

    [Fact]
    public void Convert_HandlesParagraphsAndWhitespace()
    {
        var result = HtmlToPlainText.Convert("<p>First</p><p>Second</p>");

        Assert.Equal("First\nSecond", result);
    }
}
using AutoSourcing.Services.Sms;
using Xunit;

namespace AutoSourcing.Tests;

public class Gsm7Tests
{
    [Fact]
    public void ToAscii_ConvertsSmartQuotesAndDashes()
    {
        var result = Gsm7.ToAscii("Hello \u2018world\u2019 \u2013 it\u2019s great \u2014 really!");

        Assert.Equal("Hello 'world' - it's great - really!", result);
    }

    [Fact]
    public void ToAscii_ConvertsEllipsisBulletAndArrows()
    {
        var result = Gsm7.ToAscii("Read more\u2026 \u2022 Press \u2192 here")!;

        Assert.Equal("Read more... - Press -> here", result);
    }

    [Fact]
    public void ToAscii_StripsAccentsToAsciiBaseLetters()
    {
        var result = Gsm7.ToAscii("André Çağla ünïcode");

        Assert.Equal("Andre Cagla unicode", result);
    }

    [Fact]
    public void ToAscii_RemovesEmojiAndUnmappableCharacters()
    {
        var result = Gsm7.ToAscii("Sent a \uD83D\uDE00 smiley + \u00A9 2026")!;

        Assert.Equal("Sent a  smiley + (c) 2026", result);
    }

    [Fact]
    public void ToAscii_LeavesPlainAsciiUntouched()
    {
        var result = Gsm7.ToAscii("Hi {{FirstName}}, see https://example.com?q=1&x=2");

        Assert.Equal("Hi {{FirstName}}, see https://example.com?q=1&x=2", result);
    }
}
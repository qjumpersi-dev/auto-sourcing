using AutoSourcing.Core.Entities;
using AutoSourcing.Services.Outreach;
using Xunit;

namespace AutoSourcing.Tests;

public class PersonalizationServiceTests
{
    private readonly PersonalizationService _sut = new();

    private static Lead SampleLead() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane.doe@example.com",
        Company = "Acme Corp",
        JobTitle = "Head of Talent"
    };

    [Fact]
    public void RenderTemplate_ReplacesFirstName()
    {
        var result = _sut.RenderTemplate("Hi {{FirstName}},", SampleLead());
        Assert.Equal("Hi Jane,", result);
    }

    [Fact]
    public void RenderTemplate_ReplacesCompanyWithFallback_WhenMissing()
    {
        var lead = SampleLead();
        lead.Company = null;

        var result = _sut.RenderTemplate("Join {{Company}}", lead);

        Assert.Equal("Join your company", result);
    }

    [Fact]
    public void RenderTemplate_IsCaseInsensitive_AndHandlesMultipleTokens()
    {
        var result = _sut.RenderTemplate("Hi {{firstname}} at {{COMPANY}}, you are a {{JobTitle}}.", SampleLead());

        Assert.Equal("Hi Jane at Acme Corp, you are a Head of Talent.", result);
    }

    [Fact]
    public void RenderTemplate_LeavesUnknownTokensUntouched()
    {
        var result = _sut.RenderTemplate("Hi {{Nickname}}", SampleLead());

        Assert.Equal("Hi {{Nickname}}", result);
    }
}

using AutoSourcing.Core.Entities;
using AutoSourcing.Services.Email;
using AutoSourcing.Services.Outreach;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoSourcing.Tests;

public class PersonalizationServiceTests
{
    private readonly PersonalizationService _sut = new(Options.Create(new EmailOptions { PublicBaseUrl = "https://app.test" }));

    private static Lead SampleLead() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane.doe@example.com",
        Company = "Acme Corp",
        JobTitle = "Head of Talent",
        Location = "Auckland, New Zealand"
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

    [Fact]
    public void RenderTemplate_ReplacesLocation()
    {
        var result = _sut.RenderTemplate("Based in {{Location}}", SampleLead(), jobLocation: "Auckland, New Zealand");
        Assert.Equal("Based in Auckland, New Zealand", result);
    }

    [Fact]
    public void RenderTemplate_ReplacesLeadLocation()
    {
        var result = _sut.RenderTemplate("You are in {{LeadLocation}}", SampleLead());
        Assert.Equal("You are in Auckland, New Zealand", result);
    }

    [Fact]
    public void RenderTemplate_ExpandsHiringVolumePersonalisation()
    {
        var result = _sut.RenderTemplate("<p>{{Personalisation:hiring-volume}}</p>", SampleLead());

        Assert.Equal(
            "<p>Acme Corp hires a lot of Head of Talents. The QJumpers AI Talent Sourcing Engine has one of the largest candidate pools in the world for these positions with contact details and AI Augmented skills. Larger than LinkedIn, Seek and Indeed.</p>",
            result);
    }

    [Fact]
    public void RenderTemplate_ExpandsSpecialisedSkillsPersonalisation()
    {
        var result = _sut.RenderTemplate("<p>{{Personalisation:specialised-skills}}</p>", SampleLead());

        Assert.Equal(
            "<p>Acme Corp often hires for a lot of specialised skill sets. The QJumpers AI Talent Sourcing Engine has one of the largest candidate pools in the world for these positions with contact details and AI Augmented skills. Larger than LinkedIn, Seek and Indeed.</p>",
            result);
    }

    [Fact]
    public void RenderTemplate_LeavesUnknownPersonalisationUntouched()
    {
        var result = _sut.RenderTemplate("{{Personalisation:does-not-exist}}", SampleLead());
        Assert.Equal("{{Personalisation:does-not-exist}}", result);
    }

    [Fact]
    public void GetOptions_ReturnsTwoPersonalisationOptions()
    {
        Assert.Equal(2, _sut.GetOptions().Count);
    }
}

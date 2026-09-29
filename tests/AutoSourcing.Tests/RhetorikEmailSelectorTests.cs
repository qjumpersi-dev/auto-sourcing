using AutoSourcing.Services.Rhetorik;
using Xunit;

namespace AutoSourcing.Tests;

public class RhetorikEmailSelectorTests
{
    [Fact]
    public void SelectEligible_PersonalFirstThenVerifiedBusiness()
    {
        var contact = new List<RhetorikContactEmail>
        {
            new() { Email = "jane@acmecorp.com", Type = "Business", Status = "Verified" },
            new() { Email = "jane.doe@gmail.com", Type = "Personal" },
            new() { Email = "jane@acmecorp.com", Type = "Business", Status = "Invalid" }
        };

        var result = RhetorikEmailSelector.SelectEligible(contact, profileEmails: null);

        Assert.Collection(result,
            e => Assert.Equal("jane.doe@gmail.com", e.Address),
            e => Assert.Equal("jane@acmecorp.com", e.Address));
        Assert.Equal("Personal", result[0].Type);
        Assert.Equal("Business", result[1].Type);
        Assert.True(result[1].IsVerified);
    }

    [Fact]
    public void SelectEligible_ExcludesUnverifiedBusinessAndUnknownTypes()
    {
        var contact = new List<RhetorikContactEmail>
        {
            new() { Email = "jane@acmecorp.com", Type = "Business", Status = "Unverified" },
            new() { Email = "jane@unknownmail.io", Type = "Unknown", Status = "Valid" }
        };

        var result = RhetorikEmailSelector.SelectEligible(contact, profileEmails: null);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectEligible_AcceptsProfessionalAsBusiness()
    {
        var contact = new List<RhetorikContactEmail>
        {
            new() { Email = "jane@acmecorp.com", Type = "Professional", Status = "valid" }
        };

        var result = RhetorikEmailSelector.SelectEligible(contact, profileEmails: null);

        var email = Assert.Single(result);
        Assert.Equal("jane@acmecorp.com", email.Address);
        Assert.Equal("Business", email.Type);
        Assert.True(email.IsVerified);
    }

    [Fact]
    public void SelectEligible_AddsProfileEmailFallback_WhenNoPersonalContactEmail()
    {
        var contact = new List<RhetorikContactEmail>
        {
            new() { Email = "jane@acmecorp.com", Type = "Business", Status = "Verified" }
        };
        var profile = new List<RhetorikProfileEmail>
        {
            new() { Email = "jane@acmecorp.com", Priority = 2 },
            new() { Email = "jane.personal@gmail.com", Priority = 1 }
        };

        var result = RhetorikEmailSelector.SelectEligible(contact, profile);

        Assert.Collection(result,
            e => Assert.Equal("jane.personal@gmail.com", e.Address),
            e => Assert.Equal("jane@acmecorp.com", e.Address));
        Assert.Equal("Personal", result[0].Type);
        Assert.False(result[0].IsVerified);
    }

    [Fact]
    public void SelectEligible_DoesNotAddProfileFallback_WhenPersonalContactEmailExists()
    {
        var contact = new List<RhetorikContactEmail>
        {
            new() { Email = "jane.doe@gmail.com", Type = "Personal" }
        };
        var profile = new List<RhetorikProfileEmail>
        {
            new() { Email = "jane.other@gmail.com", Priority = 1 }
        };

        var result = RhetorikEmailSelector.SelectEligible(contact, profile);

        Assert.Collection(result,
            e => Assert.Equal("jane.doe@gmail.com", e.Address));
    }

    [Fact]
    public void SelectEligible_DoesNotUseProfileEmailsForBusiness()
    {
        var profile = new List<RhetorikProfileEmail>
        {
            new() { Email = "jane@acmecorp.com", Priority = 1 }
        };

        var result = RhetorikEmailSelector.SelectEligible(contactEmails: null, profile);

        var email = Assert.Single(result);
        Assert.Equal("jane@acmecorp.com", email.Address);
        Assert.Equal("Personal", email.Type);
    }

    [Fact]
    public void SelectEligible_DeduplicatesAddresses()
    {
        var contact = new List<RhetorikContactEmail>
        {
            new() { Email = "jane.doe@gmail.com", Type = "Personal" },
            new() { Email = "JANE.DOE@GMAIL.COM", Type = "Business", Status = "Valid" }
        };

        var result = RhetorikEmailSelector.SelectEligible(contact, profileEmails: null);

        Assert.Single(result);
        Assert.Equal("jane.doe@gmail.com", result[0].Address);
    }
}
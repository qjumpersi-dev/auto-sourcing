using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class Lead : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? Location { get; set; }
    public string? LinkedInUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public ConsentChannel? PreferredChannel { get; set; }
    public string? Country { get; set; }

    // Talent-pool tags (curated) and the candidate's own details, kept for reuse across clients.
    public string? IndustryTag { get; set; }
    public string? JobTitleTag { get; set; }
    public string? RoleFamilyTag { get; set; }
    public string? SourceIndustry { get; set; }
    public string? PayExpectation { get; set; }
    public string? Availability { get; set; }
    public string? NoticePeriod { get; set; }
    public string? WorkAuthorisation { get; set; }
    public string? LicencesHeld { get; set; }

    public Guid? ConversationKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OutreachMessage> OutreachMessages { get; set; } = new List<OutreachMessage>();
    public ICollection<ChannelConsent> ChannelConsents { get; set; } = new List<ChannelConsent>();
    public ICollection<LeadEmail> Emails { get; set; } = new List<LeadEmail>();
    public ICollection<ConversationMessage> Conversations { get; set; } = new List<ConversationMessage>();
    public LeadProfile? Profile { get; set; }
}

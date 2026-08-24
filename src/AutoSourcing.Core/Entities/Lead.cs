using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class Lead
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? LinkedInUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OutreachMessage> OutreachMessages { get; set; } = new List<OutreachMessage>();
}

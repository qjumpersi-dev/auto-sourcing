namespace AutoSourcing.Core.Entities;

public class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<OutreachMessage> OutreachMessages { get; set; } = new List<OutreachMessage>();
}

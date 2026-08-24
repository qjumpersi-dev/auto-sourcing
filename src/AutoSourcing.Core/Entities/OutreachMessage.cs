namespace AutoSourcing.Core.Entities;

public class OutreachMessage
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public OutreachChannel Channel { get; set; } = OutreachChannel.Email;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public OutreachMessageStatus Status { get; set; } = OutreachMessageStatus.Draft;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}

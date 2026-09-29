using AutoSourcing.Core.Enums;

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
    public int? StepOrder { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? RepliedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    // Snapshot of the sending identity captured when the draft was created.
    public int? SentByUserId { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public string? ReplyTo { get; set; }
}

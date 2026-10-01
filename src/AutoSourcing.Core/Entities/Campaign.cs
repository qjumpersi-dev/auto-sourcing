using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class Campaign : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SubjectTemplate { get; set; }
    public string? BodyTemplate { get; set; }
    public OutreachChannel Channel { get; set; } = OutreachChannel.Email;
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public int? SequenceId { get; set; }
    public Sequence? Sequence { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<OutreachMessage> OutreachMessages { get; set; } = new List<OutreachMessage>();
}

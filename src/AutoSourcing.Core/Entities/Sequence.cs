using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class Sequence
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SequenceStatus Status { get; set; } = SequenceStatus.Draft;

    public int SendDaysMask { get; set; } = 62;
    public TimeOnly SendWindowStart { get; set; } = new(9, 0);
    public TimeOnly SendWindowEnd { get; set; } = new(17, 0);
    public bool IncludeUnsubscribe { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<SequenceStep> Steps { get; set; } = new List<SequenceStep>();
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
}
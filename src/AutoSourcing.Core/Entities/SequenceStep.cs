using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class SequenceStep
{
    public int Id { get; set; }
    public int SequenceId { get; set; }
    public Sequence Sequence { get; set; } = null!;
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public OutreachChannel Channel { get; set; } = OutreachChannel.Email;
    public string? SubjectTemplate { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public int DelayDays { get; set; }
    public SequenceStepCondition Condition { get; set; } = SequenceStepCondition.Always;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

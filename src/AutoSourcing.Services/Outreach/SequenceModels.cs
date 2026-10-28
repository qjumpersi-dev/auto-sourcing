using AutoSourcing.Core.Enums;

namespace AutoSourcing.Services.Outreach;

public class SequenceStepRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public OutreachChannel Channel { get; set; } = OutreachChannel.Email;
    public string? SubjectTemplate { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public int DelayDays { get; set; }
    public SequenceStepCondition Condition { get; set; } = SequenceStepCondition.Always;
}

public class SaveSequenceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SequenceStatus Status { get; set; } = SequenceStatus.Draft;
    public int SendDaysMask { get; set; } = 62;
    public TimeOnly SendWindowStart { get; set; } = new(9, 0);
    public TimeOnly SendWindowEnd { get; set; } = new(17, 0);
    public bool IncludeUnsubscribe { get; set; } = true;
    public List<SequenceStepRequest> Steps { get; set; } = [];
}

public record SequencePreview(string Subject, string Body);

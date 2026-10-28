namespace AutoSourcing.Services.Reports;

public record CampaignReportSummary(
    int CampaignId,
    string CampaignName,
    string? SequenceName,
    int TotalCandidates,
    List<StepReport> Steps
);

public record StepReport(
    int StepNumber,
    string StepName,
    string Channel,
    int Sent,
    int Delivered,
    int Opened,
    int Clicked,
    int Replied,
    int Failed,
    int Bounced
);

public record CandidateReportRow(
    int LeadId,
    string CandidateName,
    string Email,
    string? Phone,
    string CampaignName,
    string? SequenceName,
    string? AddedBy,
    DateTime? AddedAt,
    string CurrentStage,
    int DaysInStage,
    string EngagementStatus,
    DateTime? LastContact,
    DateTime? LastReply,
    string NextAction,
    DateTime? NextActionAt,
    string EmailStatus,
    string SmsStatus,
    bool OptOut,
    bool GoalAchieved,
    bool HumanAttention
);

public record CandidateReportResponse(
    int CampaignId,
    string CampaignName,
    List<CandidateReportRow> Candidates
);

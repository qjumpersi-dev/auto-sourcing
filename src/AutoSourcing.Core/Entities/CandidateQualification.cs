using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

// A candidate's interview-ready record for a specific job/campaign. The checks are generated from
// the job's Role brief, so the agent, the HR Assist checklist and the handover all use the same
// information.
public class CandidateQualification : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public int? JobId { get; set; }
    public int? CampaignId { get; set; }

    // Candidate-specific details captured during qualification.
    public string? PayExpectation { get; set; }
    public string? Availability { get; set; }
    public string? NoticePeriod { get; set; }

    public int? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public DateTime? HandoverSentAt { get; set; }
    public string? HandoverSentTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<QualificationCheck> Checks { get; set; } = new List<QualificationCheck>();
}

public class QualificationCheck : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public int CandidateQualificationId { get; set; }
    public CandidateQualification Qualification { get; set; } = null!;

    public InterviewReadyCriterion Criterion { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Expected { get; set; }

    public InterviewReadyStatus Status { get; set; } = InterviewReadyStatus.Unknown;
    public string? Answer { get; set; }
    public string? Notes { get; set; }

    public CheckSource Source { get; set; } = CheckSource.Agent;
    public int? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
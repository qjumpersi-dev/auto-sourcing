using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class Interview : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public int? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public int? JobId { get; set; }

    public DateTime StartAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public InterviewStatus Status { get; set; } = InterviewStatus.Proposed;

    // Microsoft Graph references so we can update/cancel and fetch the transcript later.
    public string? GraphEventId { get; set; }
    public string? OnlineMeetingId { get; set; }
    public string? TeamsJoinUrl { get; set; }

    public string? Transcript { get; set; }
    public DateTime? TranscriptFetchedAt { get; set; }

    // AI-generated summary of the interview, plus fit against the role and suggested follow-ups.
    public string? Summary { get; set; }
    public DateTime? SummaryGeneratedAt { get; set; }

    public string? Notes { get; set; }
    public DateTime? ReminderSentAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InterviewAttendee> Attendees { get; set; } = new List<InterviewAttendee>();
}

public class InterviewAttendee : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public int InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    // Set when the attendee is one of our own users (e.g. a panel member).
    public int? AppUserId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public InterviewAttendeeRole Role { get; set; } = InterviewAttendeeRole.Panel;
}

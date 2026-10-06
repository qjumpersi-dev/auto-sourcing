using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Microsoft;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Interviews;

public record InterviewSlot(DateTime StartUtc, string Label);

public interface IInterviewService
{
    // The next few times the organiser is free.
    Task<IReadOnlyList<InterviewSlot>> GetAvailableSlotsAsync(int leadId, CancellationToken cancellationToken = default);

    Task<Interview> BookAsync(int leadId, int? campaignId, int? jobId, DateTime startUtc, IReadOnlyCollection<int> panelUserIds, CancellationToken cancellationToken = default);

    Task<Interview> RescheduleAsync(int interviewId, DateTime startUtc, CancellationToken cancellationToken = default);

    Task CancelAsync(int interviewId, string? reason, CancellationToken cancellationToken = default);
}

public class InterviewService : IInterviewService
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IMicrosoftGraphService _graphService;
    private readonly IMicrosoftTokenService _tokenService;
    private readonly InterviewOptions _options;

    public InterviewService(
        AutoSourcingDbContext dbContext,
        IMicrosoftGraphService graphService,
        IMicrosoftTokenService tokenService,
        IOptions<InterviewOptions> options)
    {
        _dbContext = dbContext;
        _graphService = graphService;
        _tokenService = tokenService;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<InterviewSlot>> GetAvailableSlotsAsync(int leadId, CancellationToken cancellationToken = default)
    {
        var lead = await _dbContext.Leads
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);

        if (lead is null)
        {
            return [];
        }

        var organiser = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == lead.UserId, cancellationToken);

        if (organiser is null || string.IsNullOrWhiteSpace(organiser.MicrosoftRefreshToken))
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(_options.EarliestBookingHours);
        var windowEnd = now.AddDays(_options.LatestBookingDays);

        var accessToken = await _tokenService.GetAccessTokenAsync(organiser, cancellationToken);
        var mailbox = organiser.MicrosoftAccountEmail ?? organiser.Email;
        var busy = await _graphService.GetBusySlotsAsync(accessToken, mailbox, now, windowEnd, cancellationToken);

        var timeZone = ResolveTimeZone();
        var candidates = BuildCandidateSlots(windowStart, windowEnd, timeZone)
            .Where(slot => !OverlapsBusy(slot, busy))
            .ToList();

        // Prefer spreading the options across different days.
        var picked = new List<DateTime>();
        var usedDays = new HashSet<DateTime>();

        foreach (var slot in candidates)
        {
            var day = TimeZoneInfo.ConvertTimeFromUtc(slot, timeZone).Date;
            if (usedDays.Add(day))
            {
                picked.Add(slot);
            }

            if (picked.Count >= _options.SlotCount)
            {
                return ToSlots(picked, timeZone);
            }
        }

        // Not enough distinct days - fill up with same-day options.
        foreach (var slot in candidates)
        {
            if (picked.Contains(slot))
            {
                continue;
            }

            picked.Add(slot);
            if (picked.Count >= _options.SlotCount)
            {
                break;
            }
        }

        picked.Sort();
        return ToSlots(picked, timeZone);
    }

    private static IReadOnlyList<InterviewSlot> ToSlots(IEnumerable<DateTime> utcSlots, TimeZoneInfo timeZone) =>
        utcSlots
            .Select(utc =>
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
                return new InterviewSlot(utc, local.ToString("dddd d MMMM, h:mm tt", System.Globalization.CultureInfo.InvariantCulture));
            })
            .ToList();

    public async Task<Interview> BookAsync(int leadId, int? campaignId, int? jobId, DateTime startUtc, IReadOnlyCollection<int> panelUserIds, CancellationToken cancellationToken = default)
    {
        var lead = await _dbContext.Leads
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken)
            ?? throw new InvalidOperationException("Candidate not found.");

        var organiser = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == lead.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Organiser not found.");

        if (string.IsNullOrWhiteSpace(organiser.MicrosoftRefreshToken))
        {
            throw new InvalidOperationException("The organiser has not connected Microsoft 365.");
        }

        var duration = _options.DurationMinutes;
        var endUtc = startUtc.AddMinutes(duration);

        var attendees = new List<MeetingAttendee>
        {
            new(lead.Email, $"{lead.FirstName} {lead.LastName}".Trim())
        };

        var panel = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => panelUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var member in panel)
        {
            attendees.Add(new MeetingAttendee(member.Email, member.DisplayName));
        }

        var job = jobId is { } id
            ? await _dbContext.Jobs.IgnoreQueryFilters().FirstOrDefaultAsync(j => j.Id == id, cancellationToken)
            : null;

        var title = job is null ? "Interview" : $"Interview: {job.Title}";
        var body =
            $"<p>Hi {lead.FirstName},</p>" +
            $"<p>Your interview{(job is null ? string.Empty : $" for the {job.Title} role")} is confirmed for " +
            $"{startUtc:dddd d MMMM yyyy HH:mm} UTC ({duration} minutes).</p>" +
            "<p>Join with the Teams link in this invitation.</p>";

        var accessToken = await _tokenService.GetAccessTokenAsync(organiser, cancellationToken);
        var meeting = await _graphService.CreateOnlineMeetingAsync(accessToken, title, body, startUtc, endUtc, attendees, cancellationToken);

        var interview = new Interview
        {
            UserId = organiser.Id,
            LeadId = lead.Id,
            CampaignId = campaignId,
            JobId = jobId,
            StartAt = startUtc,
            DurationMinutes = duration,
            Status = InterviewStatus.Booked,
            GraphEventId = meeting.EventId,
            TeamsJoinUrl = meeting.JoinUrl
        };

        interview.Attendees.Add(new InterviewAttendee
        {
            UserId = organiser.Id,
            Email = lead.Email,
            Name = $"{lead.FirstName} {lead.LastName}".Trim(),
            Role = InterviewAttendeeRole.Candidate
        });

        interview.Attendees.Add(new InterviewAttendee
        {
            UserId = organiser.Id,
            AppUserId = organiser.Id,
            Email = organiser.Email,
            Name = organiser.DisplayName,
            Role = InterviewAttendeeRole.Organiser
        });

        foreach (var member in panel)
        {
            interview.Attendees.Add(new InterviewAttendee
            {
                UserId = organiser.Id,
                AppUserId = member.Id,
                Email = member.Email,
                Name = member.DisplayName,
                Role = InterviewAttendeeRole.Panel
            });
        }

        _dbContext.Interviews.Add(interview);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return interview;
    }

    public async Task<Interview> RescheduleAsync(int interviewId, DateTime startUtc, CancellationToken cancellationToken = default)
    {
        var interview = await _dbContext.Interviews
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken)
            ?? throw new InvalidOperationException("Interview not found.");

        var organiser = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == interview.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Organiser not found.");

        if (!string.IsNullOrWhiteSpace(interview.GraphEventId))
        {
            var accessToken = await _tokenService.GetAccessTokenAsync(organiser, cancellationToken);
            await _graphService.RescheduleMeetingAsync(
                accessToken, interview.GraphEventId, startUtc, startUtc.AddMinutes(interview.DurationMinutes), cancellationToken);
        }

        interview.StartAt = startUtc;
        interview.Status = InterviewStatus.Booked;
        interview.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return interview;
    }

    public async Task CancelAsync(int interviewId, string? reason, CancellationToken cancellationToken = default)
    {
        var interview = await _dbContext.Interviews
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken)
            ?? throw new InvalidOperationException("Interview not found.");

        var organiser = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == interview.UserId, cancellationToken);

        if (organiser is not null && !string.IsNullOrWhiteSpace(interview.GraphEventId))
        {
            var accessToken = await _tokenService.GetAccessTokenAsync(organiser, cancellationToken);
            await _graphService.CancelMeetingAsync(accessToken, interview.GraphEventId, reason, cancellationToken);
        }

        interview.Status = InterviewStatus.Cancelled;
        interview.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IEnumerable<DateTime> BuildCandidateSlots(DateTime windowStart, DateTime windowEnd, TimeZoneInfo timeZone)
    {
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(windowStart, timeZone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(windowEnd, timeZone);

        for (var day = localStart.Date; day <= localEnd.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            for (var minutes = _options.WorkingHourStart * 60;
                 minutes + _options.DurationMinutes <= _options.WorkingHourEnd * 60;
                 minutes += _options.DurationMinutes)
            {
                var local = DateTime.SpecifyKind(day.AddMinutes(minutes), DateTimeKind.Unspecified);
                DateTime utc;
                try
                {
                    utc = TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                if (utc < windowStart || utc > windowEnd)
                {
                    continue;
                }

                yield return utc;
            }
        }
    }

    private bool OverlapsBusy(DateTime slotStart, IReadOnlyList<BusySlot> busy)
    {
        var slotEnd = slotStart.AddMinutes(_options.DurationMinutes);
        return busy.Any(b => slotStart < b.EndUtc && slotEnd > b.StartUtc);
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}

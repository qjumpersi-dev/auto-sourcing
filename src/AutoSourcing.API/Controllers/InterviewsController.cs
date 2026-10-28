using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Interviews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class BookInterviewRequest
{
    public DateTime StartAt { get; set; }
    public int? CampaignId { get; set; }
    public int? JobId { get; set; }
    public List<int> PanelUserIds { get; set; } = [];
}

public class RescheduleInterviewRequest
{
    public DateTime StartAt { get; set; }
}

public class CancelInterviewRequest
{
    public string? Reason { get; set; }
}

public class SetInterviewRoleRequest
{
    public int? JobId { get; set; }
}

[ApiController]
[Route("api")]
public class InterviewsController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IInterviewService _interviewService;

    public InterviewsController(AutoSourcingDbContext dbContext, IInterviewService interviewService)
    {
        _dbContext = dbContext;
        _interviewService = interviewService;
    }

    // Upcoming and recent interviews for the signed-in user.
    [HttpGet("interviews")]
    public async Task<ActionResult> Upcoming(CancellationToken cancellationToken)
    {
        var interviews = await _dbContext.Interviews
            .Include(i => i.Lead)
            .AsNoTracking()
            .Where(i => i.StartAt >= DateTime.UtcNow.AddDays(-30) && i.Status != InterviewStatus.Cancelled)
            .OrderBy(i => i.StartAt)
            .ToListAsync(cancellationToken);

        return Ok(interviews.Select(ToDto));
    }

    [HttpGet("leads/{leadId:int}/interviews")]
    public async Task<ActionResult> ForLead(int leadId, CancellationToken cancellationToken)
    {
        var interviews = await _dbContext.Interviews
            .Include(i => i.Lead)
            .AsNoTracking()
            .Where(i => i.LeadId == leadId)
            .OrderByDescending(i => i.StartAt)
            .ToListAsync(cancellationToken);

        return Ok(interviews.Select(ToDto));
    }

    [HttpGet("leads/{leadId:int}/interview-slots")]
    public async Task<ActionResult> Slots(int leadId, CancellationToken cancellationToken)
    {
        var slots = await _interviewService.GetAvailableSlotsAsync(leadId, cancellationToken);
        return Ok(slots.Select(s => new { startUtc = s.StartUtc, label = s.Label }));
    }

    [HttpPost("leads/{leadId:int}/interviews")]
    public async Task<ActionResult> Book(int leadId, [FromBody] BookInterviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var interview = await _interviewService.BookAsync(
                leadId,
                request.CampaignId,
                request.JobId,
                request.StartAt.ToUniversalTime(),
                request.PanelUserIds ?? [],
                cancellationToken);

            return Ok(ToDto(interview));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("interviews/{id:int}")]
    public async Task<ActionResult> Reschedule(int id, [FromBody] RescheduleInterviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var interview = await _interviewService.RescheduleAsync(id, request.StartAt.ToUniversalTime(), cancellationToken);
            return Ok(ToDto(interview));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("interviews/{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id, [FromBody] CancelInterviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _interviewService.CancelAsync(id, request?.Reason, cancellationToken);
            return Ok(new { cancelled = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("interviews/{id:int}/role")]
    public async Task<ActionResult> SetRole(int id, [FromBody] SetInterviewRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _interviewService.SetRoleAsync(id, request?.JobId, cancellationToken);
            return Ok(new { role = request?.JobId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Pull the Teams transcript and generate the AI summary on demand (otherwise this happens
    // automatically in the background once the interview has finished).
    [HttpPost("interviews/{id:int}/process-transcript")]
    public async Task<ActionResult> ProcessTranscript(int id, CancellationToken cancellationToken)
    {
        var processed = await _interviewService.ProcessTranscriptAsync(id, cancellationToken);
        return Ok(new { processed });
    }

    private static object ToDto(Core.Entities.Interview interview) => new
    {
        interview.Id,
        interview.LeadId,
        CandidateName = interview.Lead is null ? null : $"{interview.Lead.FirstName} {interview.Lead.LastName}".Trim(),
        interview.CampaignId,
        interview.JobId,
        interview.StartAt,
        interview.DurationMinutes,
        Status = interview.Status.ToString(),
        interview.TeamsJoinUrl,
        interview.Summary,
        interview.Attended,
        HasTranscript = interview.Transcript != null
    };
}

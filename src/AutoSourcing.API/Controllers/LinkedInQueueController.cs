using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Email;
using AutoSourcing.Services.Outreach;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class LinkedInQueueResult
{
    public bool Sent { get; set; }
    public string? Error { get; set; }
}

public class LinkedInQueueItem
{
    public int MessageId { get; set; }
    public int CampaignId { get; set; }
    public int LeadId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string? ProfileUrl { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
}

// Used by the local LinkedIn worker: it fetches queued InMails, sends them from a machine with a
// browser and a signed-in LinkedIn session, then reports the result back.
[ApiController]
[Route("api/linkedin/queue")]
public class LinkedInQueueController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IEmailTrackingService _emailTracking;

    public LinkedInQueueController(AutoSourcingDbContext dbContext, IEmailTrackingService emailTracking)
    {
        _dbContext = dbContext;
        _emailTracking = emailTracking;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LinkedInQueueItem>>> GetQueue(CancellationToken cancellationToken)
    {
        var items = await _dbContext.OutreachMessages
            .Where(m => m.Channel == OutreachChannel.LinkedIn && m.Status == OutreachMessageStatus.Queued)
            .Include(m => m.Lead)
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Select(m => new
            {
                m.Id,
                m.CampaignId,
                m.LeadId,
                m.Lead.FirstName,
                m.Lead.LastName,
                m.Lead.LinkedInUrl,
                m.Subject,
                m.Body
            })
            .ToListAsync(cancellationToken);

        var result = items.Select(m => new LinkedInQueueItem
        {
            MessageId = m.Id,
            CampaignId = m.CampaignId,
            LeadId = m.LeadId,
            CandidateName = $"{m.FirstName} {m.LastName}".Trim(),
            ProfileUrl = m.LinkedInUrl,
            Subject = m.Subject,
            // Tracked like email. LinkedIn prefetching used to create false clicks; that's now
            // filtered by the headless-browser check and the "too soon after send" guard.
            Body = _emailTracking.RewriteLinksInPlainText(HtmlToPlainText.Convert(m.Body), m.Id)
        }).ToList();

        return Ok(result);
    }

    [HttpPost("{messageId:int}")]
    public async Task<ActionResult> ReportResult(int messageId, [FromBody] LinkedInQueueResult result, CancellationToken cancellationToken)
    {
        var message = await _dbContext.OutreachMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message is null)
        {
            return NotFound(new { error = $"Message {messageId} not found." });
        }

        if (result.Sent)
        {
            message.Status = OutreachMessageStatus.Sent;
            message.SentAt = DateTime.UtcNow;
            message.ErrorMessage = null;
        }
        else
        {
            message.Status = OutreachMessageStatus.Failed;
            message.ErrorMessage = string.IsNullOrWhiteSpace(result.Error) ? "LinkedIn send failed." : result.Error;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { status = message.Status.ToString() });
    }
}

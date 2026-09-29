using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Outreach;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class CreateDraftRequest
{
    public int LeadId { get; set; }
    public int CampaignId { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public OutreachChannel Channel { get; set; } = OutreachChannel.Email;
}

[ApiController]
[Route("api/campaigns/{campaignId:int}/messages")]
public class OutreachMessagesController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IOutreachService _outreachService;

    public OutreachMessagesController(AutoSourcingDbContext dbContext, IOutreachService outreachService)
    {
        _dbContext = dbContext;
        _outreachService = outreachService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OutreachMessage>>> GetMessages(int campaignId, CancellationToken cancellationToken)
    {
        var messages = await _dbContext.OutreachMessages
            .Where(m => m.CampaignId == campaignId)
            .Include(m => m.Lead)
            .AsNoTracking()
            .OrderBy(m => m.LeadId)
            .ThenBy(m => m.StepOrder)
            .ThenBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.LeadId,
                m.CampaignId,
                m.Channel,
                m.Subject,
                m.Body,
                m.Status,
                m.ErrorMessage,
                m.StepOrder,
                m.OpenedAt,
                m.ClickedAt,
                m.RepliedAt,
                m.CreatedAt,
                m.SentAt,
                m.SentByUserId,
                m.FromAddress,
                m.FromName,
                m.ReplyTo,
                Lead = new
                {
                    m.Lead.Id,
                    m.Lead.FirstName,
                    m.Lead.LastName,
                    m.Lead.Email,
                    m.Lead.Phone,
                    m.Lead.Company,
                    m.Lead.JobTitle,
                    m.Lead.Location,
                    m.Lead.LinkedInUrl,
                    m.Lead.Source,
                    m.Lead.ExternalId,
                    m.Lead.Status,
                    m.Lead.PreferredChannel,
                    m.Lead.Country,
                    m.Lead.CreatedAt,
                    m.Lead.UpdatedAt
                }
            })
            .ToListAsync(cancellationToken);

        return Ok(messages);
    }

    [HttpPost("drafts")]
    public async Task<ActionResult<OutreachMessage>> CreateDraft(int campaignId, [FromBody] CreateDraftRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var message = await _outreachService.CreateDraftAsync(
                request.LeadId, campaignId, request.SubjectTemplate, request.BodyTemplate, request.Channel, cancellationToken);
            return CreatedAtAction(nameof(GetMessages), new { campaignId }, new
            {
                message.Id,
                message.LeadId,
                message.CampaignId,
                message.Channel,
                message.Subject,
                message.Body,
                message.Status,
                message.ErrorMessage,
                message.StepOrder,
                message.CreatedAt,
                message.SentAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{messageId:int}/send")]
    public async Task<IActionResult> SendMessage(int campaignId, int messageId, CancellationToken cancellationToken)
    {
        var message = await _dbContext.OutreachMessages
            .Include(m => m.Lead)
                .ThenInclude(l => l.Emails)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.CampaignId == campaignId, cancellationToken);

        if (message is null)
        {
            return NotFound();
        }

        if (message.Lead.Status == LeadStatus.OptedOut)
        {
            return BadRequest(new { error = "Lead has opted out." });
        }

        try
        {
            var result = await _outreachService.SendMessageAsync(message, cancellationToken);
            if (!result.Sent)
            {
                return Ok(new { dryRun = true, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            message.Status = OutreachMessageStatus.Failed;
            message.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{messageId:int}/mark-replied")]
    public async Task<IActionResult> MarkReplied(int campaignId, int messageId, CancellationToken cancellationToken)
    {
        var message = await _dbContext.OutreachMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.CampaignId == campaignId, cancellationToken);

        if (message is null)
        {
            return NotFound();
        }

        message.RepliedAt ??= DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

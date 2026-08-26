using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Email;
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
}

[ApiController]
[Route("api/campaigns/{campaignId:int}/messages")]
public class OutreachMessagesController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IOutreachService _outreachService;
    private readonly IEmailService _emailService;

    public OutreachMessagesController(
        AutoSourcingDbContext dbContext,
        IOutreachService outreachService,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _outreachService = outreachService;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OutreachMessage>>> GetMessages(int campaignId, CancellationToken cancellationToken)
    {
        var messages = await _dbContext.OutreachMessages
            .Where(m => m.CampaignId == campaignId)
            .Include(m => m.Lead)
            .AsNoTracking()
            .OrderBy(m => m.CreatedAt)
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
                m.CreatedAt,
                m.SentAt,
                Lead = new
                {
                    m.Lead.Id,
                    m.Lead.FirstName,
                    m.Lead.LastName,
                    m.Lead.Email,
                    m.Lead.Company,
                    m.Lead.JobTitle,
                    m.Lead.Status
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
                request.LeadId, campaignId, request.SubjectTemplate, request.BodyTemplate, cancellationToken);
            return CreatedAtAction(nameof(GetMessages), new { campaignId }, message);
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
            .FirstOrDefaultAsync(m => m.Id == messageId && m.CampaignId == campaignId, cancellationToken);

        if (message is null)
        {
            return NotFound();
        }

        if (message.Channel != OutreachChannel.Email)
        {
            return BadRequest(new { error = "Only email sending is currently supported." });
        }

        if (message.Lead.Status == LeadStatus.OptedOut)
        {
            return BadRequest(new { error = "Lead has opted out." });
        }

        try
        {
            await _emailService.SendAsync(message.Lead.Email, message.Subject ?? "(no subject)", message.Body, cancellationToken);
            message.Status = OutreachMessageStatus.Sent;
            message.SentAt = DateTime.UtcNow;
            message.ErrorMessage = null;

            if (message.Lead.Status == LeadStatus.New)
            {
                message.Lead.Status = LeadStatus.Contacted;
                message.Lead.UpdatedAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            message.Status = OutreachMessageStatus.Failed;
            message.ErrorMessage = ex.Message;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

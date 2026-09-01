using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Email;
using AutoSourcing.Services.LinkedIn;
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
    private readonly IEmailService _emailService;
    private readonly ILinkedInService _linkedInService;

    public OutreachMessagesController(
        AutoSourcingDbContext dbContext,
        IOutreachService outreachService,
        IEmailService emailService,
        ILinkedInService linkedInService)
    {
        _dbContext = dbContext;
        _outreachService = outreachService;
        _emailService = emailService;
        _linkedInService = linkedInService;
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
            switch (message.Channel)
            {
                case OutreachChannel.Email:
                    await _emailService.SendAsync(message.Lead.Email, message.Subject ?? "(no subject)", message.Body, cancellationToken);
                    break;

                case OutreachChannel.LinkedIn:
                    if (string.IsNullOrWhiteSpace(message.Lead.LinkedInUrl))
                    {
                        return BadRequest(new { error = "Lead has no LinkedIn URL." });
                    }

                    var result = await _linkedInService.SendInMailAsync(message.Lead.LinkedInUrl, message.Subject ?? string.Empty, message.Body, cancellationToken);
                    if (!result.Sent)
                    {
                        return Ok(new { dryRun = true, message = result.Message });
                    }
                    break;

                default:
                    return BadRequest(new { error = "Sending via this channel is not yet supported." });
            }

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
            message.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

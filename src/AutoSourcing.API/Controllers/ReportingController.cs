using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportingController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;

    public ReportingController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("campaign/{campaignId:int}")]
    public async Task<ActionResult<CampaignReportSummary>> GetCampaignSummary(int campaignId, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns
            .Include(c => c.Sequence).ThenInclude(s => s!.Steps)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return NotFound();
        }

        var messages = await _dbContext.OutreachMessages
            .AsNoTracking()
            .Where(m => m.CampaignId == campaignId)
            .ToListAsync(cancellationToken);

        var totalCandidates = messages.Select(m => m.LeadId).Distinct().Count();

        var steps = (campaign.Sequence?.Steps ?? [])
            .OrderBy(s => s.Order)
            .Select(s =>
            {
                var stepMessages = messages.Where(m => m.StepOrder == s.Order + 1).ToList();
                return new StepReport(
                    s.Order + 1,
                    s.Name,
                    s.Channel.ToString(),
                    stepMessages.Count(m => m.Status == OutreachMessageStatus.Sent),
                    stepMessages.Count(m => m.Status == OutreachMessageStatus.Sent && m.SentAt != null),
                    stepMessages.Count(m => m.OpenedAt != null),
                    stepMessages.Count(m => m.ClickedAt != null),
                    stepMessages.Count(m => m.RepliedAt != null),
                    stepMessages.Count(m => m.Status == OutreachMessageStatus.Failed),
                    stepMessages.Count(m => m.Status == OutreachMessageStatus.Bounced)
                );
            })
            .ToList();

        return Ok(new CampaignReportSummary(
            campaign.Id,
            campaign.Name,
            campaign.Sequence?.Name,
            totalCandidates,
            steps
        ));
    }

    [HttpGet("campaign/{campaignId:int}/candidates")]
    public async Task<ActionResult<CandidateReportResponse>> GetCandidateReport(int campaignId, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns
            .Include(c => c.Sequence).ThenInclude(s => s!.Steps)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return NotFound();
        }

        var messages = await _dbContext.OutreachMessages
            .AsNoTracking()
            .Where(m => m.CampaignId == campaignId)
            .Include(m => m.Lead)
            .ToListAsync(cancellationToken);

        var consents = await _dbContext.ChannelConsents
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var steps = (campaign.Sequence?.Steps ?? []).OrderBy(s => s.Order).ToList();
        var totalSteps = steps.Count;

        var rows = messages
            .GroupBy(m => m.LeadId)
            .Select(group =>
            {
                var leadMessages = group.ToList();
                var lead = leadMessages.First().Lead;
                var leadConsents = consents.Where(c => c.LeadId == lead.Id).ToList();

                var addedAt = leadMessages.Min(m => m.CreatedAt);
                var currentStep = leadMessages.Max(m => m.StepOrder ?? 0);
                var currentStageMessage = leadMessages
                    .Where(m => m.StepOrder == currentStep)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();
                var daysInStage = currentStageMessage != null
                    ? (int)(now - currentStageMessage.CreatedAt).TotalDays
                    : 0;

                var lastSent = leadMessages
                    .Where(m => m.SentAt != null)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault()?.SentAt;

                var lastReply = leadMessages
                    .Where(m => m.RepliedAt != null)
                    .OrderByDescending(m => m.RepliedAt)
                    .FirstOrDefault()?.RepliedAt;

                var hasEngagement = leadMessages.Any(m => m.RepliedAt != null || m.ClickedAt != null);
                var hasOpened = leadMessages.Any(m => m.OpenedAt != null);
                var engagementStatus = hasEngagement ? "Engaged" : hasOpened ? "Waiting" : "Not engaged";

                var nextStepIndex = currentStep;
                string nextAction;
                DateTime? nextActionAt = null;
                if (nextStepIndex < totalSteps)
                {
                    var nextStep = steps[nextStepIndex];
                    nextAction = $"Step {nextStepIndex + 1}: {nextStep.Name}";
                    if (currentStageMessage?.SentAt != null)
                    {
                        nextActionAt = currentStageMessage.SentAt.Value.AddDays(Math.Max(0, nextStep.DelayDays));
                    }
                }
                else
                {
                    nextAction = "Complete";
                }

                var emailMsgs = leadMessages.Where(m => m.Channel == OutreachChannel.Email).ToList();
                var smsMsgs = leadMessages.Where(m => m.Channel == OutreachChannel.Sms).ToList();

                string ChannelStatus(List<Core.Entities.OutreachMessage> msgs)
                {
                    if (msgs.Count == 0) return "—";
                    var latest = msgs.OrderByDescending(m => m.CreatedAt).First();
                    return latest.Status switch
                    {
                        OutreachMessageStatus.Sent => "Sent",
                        OutreachMessageStatus.Failed => "Failed",
                        OutreachMessageStatus.Bounced => "Bounced",
                        OutreachMessageStatus.Draft => "Draft",
                        _ => latest.Status.ToString()
                    };
                }

                var optOut = leadConsents.Any(c => c.Status == ConsentStatus.OptedOut || c.Status == ConsentStatus.DoNotContact);
                var humanAttention = leadMessages.Any(m => m.Status == OutreachMessageStatus.Failed || m.Status == OutreachMessageStatus.Bounced) || optOut;

                return new CandidateReportRow(
                    lead.Id,
                    $"{lead.FirstName} {lead.LastName}",
                    lead.Email,
                    lead.Phone,
                    campaign.Name,
                    campaign.Sequence?.Name,
                    null,
                    addedAt,
                    currentStep > 0 ? $"Step {currentStep} of {totalSteps}" : "Not started",
                    daysInStage,
                    engagementStatus,
                    lastSent,
                    lastReply,
                    nextAction,
                    nextActionAt,
                    ChannelStatus(emailMsgs),
                    ChannelStatus(smsMsgs),
                    optOut,
                    false,
                    humanAttention
                );
            })
            .OrderBy(r => r.CandidateName)
            .ToList();

        return Ok(new CandidateReportResponse(campaignId, campaign.Name, rows));
    }
}

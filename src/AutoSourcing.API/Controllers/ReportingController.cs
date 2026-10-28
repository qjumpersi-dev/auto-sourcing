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

    [HttpGet("talent-funnel")]
    public async Task<ActionResult<TalentFunnelReport>> GetTalentFunnel(
        [FromQuery] int? clientId,
        [FromQuery] string? roleFamily,
        [FromQuery] string? industry,
        CancellationToken cancellationToken)
    {
        var jobsQuery = _dbContext.Jobs.AsNoTracking();
        if (clientId is int filterClient)
        {
            jobsQuery = jobsQuery.Where(j => j.ClientId == filterClient);
        }
        if (!string.IsNullOrWhiteSpace(roleFamily))
        {
            jobsQuery = jobsQuery.Where(j => j.RoleFamily == roleFamily);
        }
        if (!string.IsNullOrWhiteSpace(industry))
        {
            jobsQuery = jobsQuery.Where(j => j.Industry == industry);
        }

        var jobs = await jobsQuery
            .Select(j => new JobScope(j.Id, j.RoleFamily, j.Industry, j.ClientId))
            .ToListAsync(cancellationToken);

        var jobIds = jobs.Select(j => j.Id).ToList();
        if (jobIds.Count == 0)
        {
            return Ok(EmptyFunnel());
        }

        var jobById = jobs.ToDictionary(j => j.Id);

        var clientIds = jobs.Where(j => j.ClientId is not null).Select(j => j.ClientId!.Value).Distinct().ToList();
        var clientNames = await _dbContext.Clients.AsNoTracking()
            .Where(c => clientIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);
        var clientNameById = clientNames.ToDictionary(c => c.Id, c => c.Name);

        var membership = await _dbContext.OutreachMessages.AsNoTracking()
            .Where(m => m.Campaign != null && m.Campaign.JobId != null && jobIds.Contains(m.Campaign.JobId.Value))
            .Select(m => new { m.LeadId, JobId = m.Campaign!.JobId!.Value, m.CreatedAt })
            .ToListAsync(cancellationToken);

        var leadIds = membership.Select(m => m.LeadId).Distinct().ToList();
        if (leadIds.Count == 0)
        {
            return Ok(EmptyFunnel());
        }

        var jobByLead = membership
            .GroupBy(m => m.LeadId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.CreatedAt).First().JobId);

        var sentLeads = (await _dbContext.OutreachMessages.AsNoTracking()
            .Where(m => leadIds.Contains(m.LeadId) && m.Status == OutreachMessageStatus.Sent)
            .Select(m => m.LeadId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        var engagedLeads = (await _dbContext.OutreachMessages.AsNoTracking()
            .Where(m => leadIds.Contains(m.LeadId) && m.RepliedAt != null)
            .Select(m => m.LeadId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        var qualifications = await _dbContext.CandidateQualifications.AsNoTracking()
            .Include(q => q.Checks)
            .Where(q => leadIds.Contains(q.LeadId) && q.JobId != null && jobIds.Contains(q.JobId.Value))
            .ToListAsync(cancellationToken);

        var qualifiedLeads = new HashSet<int>();
        var readyLeads = new HashSet<int>();
        foreach (var qualification in qualifications)
        {
            var checks = qualification.Checks.ToList();
            var agentCriteria = checks
                .Where(c => c.Criterion != InterviewReadyCriterion.HumanConfirmation
                            && c.Criterion != InterviewReadyCriterion.InterviewArranged)
                .ToList();

            if (agentCriteria.Count > 0 && agentCriteria.All(IsSatisfied))
            {
                qualifiedLeads.Add(qualification.LeadId);
            }
            if (checks.Count > 0 && checks.All(IsSatisfied))
            {
                readyLeads.Add(qualification.LeadId);
            }
        }

        var scheduledLeads = (await _dbContext.Interviews.AsNoTracking()
            .Where(i => leadIds.Contains(i.LeadId) && i.Status != InterviewStatus.Cancelled)
            .Select(i => i.LeadId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        var heldLeads = (await _dbContext.Interviews.AsNoTracking()
            .Where(i => leadIds.Contains(i.LeadId) && i.Attended == true)
            .Select(i => i.LeadId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        // Furthest stage each candidate reached.
        var stageByLead = leadIds.ToDictionary(leadId => leadId, leadId =>
        {
            if (heldLeads.Contains(leadId)) return 6;
            if (scheduledLeads.Contains(leadId)) return 5;
            if (readyLeads.Contains(leadId)) return 4;
            if (qualifiedLeads.Contains(leadId)) return 3;
            if (engagedLeads.Contains(leadId)) return 2;
            if (sentLeads.Contains(leadId)) return 1;
            return 0;
        });

        FunnelCounts Counts(IEnumerable<int> ids)
        {
            var list = ids.ToList();
            int AtLeast(int stage) => list.Count(id => stageByLead[id] >= stage);
            return new FunnelCounts(
                AtLeast(0), AtLeast(1), AtLeast(2), AtLeast(3), AtLeast(4), AtLeast(5), AtLeast(6));
        }

        var overall = Counts(leadIds);

        List<FunnelGroup> Group(Func<int, string> keySelector) =>
            leadIds
                .GroupBy(keySelector)
                .Select(g => new FunnelGroup(g.Key, Counts(g)))
                .OrderByDescending(g => g.Counts.Identified)
                .ToList();

        string KeyOf(int leadId, Func<JobScope, string?> selector)
        {
            if (!jobByLead.TryGetValue(leadId, out var jobId) || !jobById.TryGetValue(jobId, out var job))
            {
                return "Unassigned";
            }
            return selector(job) ?? "Unassigned";
        }

        var byClient = Group(leadId =>
        {
            if (!jobByLead.TryGetValue(leadId, out var jobId) || !jobById.TryGetValue(jobId, out var job) || job.ClientId is not int c)
            {
                return "Unassigned";
            }
            return clientNameById.TryGetValue(c, out var name) ? name : "Unassigned";
        });

        var byRoleFamily = Group(leadId => KeyOf(leadId, job => job.RoleFamily));
        var byIndustry = Group(leadId => KeyOf(leadId, job => job.Industry));

        return Ok(new TalentFunnelReport(overall, byClient, byRoleFamily, byIndustry));
    }

    private static bool IsSatisfied(Core.Entities.QualificationCheck check) =>
        check.Status is InterviewReadyStatus.Met or InterviewReadyStatus.NotApplicable;

    private static TalentFunnelReport EmptyFunnel() =>
        new(new FunnelCounts(0, 0, 0, 0, 0, 0, 0), [], [], []);

    private record JobScope(int Id, string? RoleFamily, string? Industry, int? ClientId);
}

public record FunnelCounts(
    int Identified,
    int Contacted,
    int Engaged,
    int Qualified,
    int InterviewReady,
    int InterviewScheduled,
    int InterviewHeld);

public record FunnelGroup(string Key, FunnelCounts Counts);

public record TalentFunnelReport(
    FunnelCounts Overall,
    IReadOnlyList<FunnelGroup> ByClient,
    IReadOnlyList<FunnelGroup> ByRoleFamily,
    IReadOnlyList<FunnelGroup> ByIndustry);

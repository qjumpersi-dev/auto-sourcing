using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Auth;
using AutoSourcing.Services.Email;
using AutoSourcing.Services.LinkedIn;
using AutoSourcing.Services.Rhetorik;
using AutoSourcing.Services.Sms;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Services.Outreach;

public class OutreachService : IOutreachService
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IPersonalizationService _personalization;
    private readonly IEmailService _emailService;
    private readonly ILinkedInService _linkedInService;
    private readonly IUnsubscribeService _unsubscribeService;
    private readonly IEmailTrackingService _emailTracking;
    private readonly IRhetorikClient _rhetorikClient;
    private readonly ISmsService _smsService;
    private readonly ISenderProvider _senderProvider;

    public OutreachService(
        AutoSourcingDbContext dbContext,
        IPersonalizationService personalization,
        IEmailService emailService,
        ILinkedInService linkedInService,
        IUnsubscribeService unsubscribeService,
        IEmailTrackingService emailTracking,
        IRhetorikClient rhetorikClient,
        ISmsService smsService,
        ISenderProvider senderProvider)
    {
        _dbContext = dbContext;
        _personalization = personalization;
        _emailService = emailService;
        _linkedInService = linkedInService;
        _unsubscribeService = unsubscribeService;
        _emailTracking = emailTracking;
        _rhetorikClient = rhetorikClient;
        _smsService = smsService;
        _senderProvider = senderProvider;
    }

    public async Task<OutreachMessage> CreateDraftAsync(int leadId, int campaignId, string subjectTemplate, string bodyTemplate, OutreachChannel channel, CancellationToken cancellationToken = default)
    {
        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken)
            ?? throw new InvalidOperationException($"Lead {leadId} not found.");

        var campaignExists = await _dbContext.Campaigns.AnyAsync(c => c.Id == campaignId, cancellationToken);
        if (!campaignExists)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found.");
        }

        if (lead.Status == LeadStatus.OptedOut)
        {
            throw new InvalidOperationException($"Lead {leadId} has opted out of outreach.");
        }

        var message = BuildDraft(lead, campaignId, subjectTemplate, bodyTemplate, channel, null);

        _dbContext.OutreachMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task<List<OutreachMessage>> AddLeadsToCampaignAsync(int campaignId, IReadOnlyCollection<int> leadIds, CancellationToken cancellationToken = default)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        if (campaign.SequenceId is null)
        {
            throw new InvalidOperationException("Add a sequence to this campaign before adding candidates.");
        }

        var firstStep = await _dbContext.SequenceSteps
            .Where(s => s.SequenceId == campaign.SequenceId.Value)
            .OrderBy(s => s.Order)
            .FirstOrDefaultAsync(cancellationToken);

        if (firstStep is null)
        {
            throw new InvalidOperationException("The campaign's sequence has no steps yet.");
        }

        var leads = await _dbContext.Leads
            .Where(l => leadIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        var existingLeadIds = await _dbContext.OutreachMessages
            .Where(m => m.CampaignId == campaignId && leadIds.Contains(m.LeadId))
            .Select(m => m.LeadId)
            .ToListAsync(cancellationToken);

        var created = new List<OutreachMessage>();

        foreach (var lead in leads)
        {
            if (lead.Status == LeadStatus.OptedOut || existingLeadIds.Contains(lead.Id))
            {
                continue;
            }

            created.Add(BuildDraft(
                lead,
                campaignId,
                firstStep.SubjectTemplate ?? string.Empty,
                firstStep.BodyTemplate,
                firstStep.Channel,
                firstStep.Order + 1));
        }

        if (created.Count > 0)
        {
            _dbContext.OutreachMessages.AddRange(created);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    public async Task<MessageSendResult> SendMessageAsync(OutreachMessage message, CancellationToken cancellationToken = default)
    {
        var consentChannel = message.Channel switch
        {
            OutreachChannel.Email => ConsentChannel.Email,
            OutreachChannel.Sms => ConsentChannel.Sms,
            OutreachChannel.WhatsApp => ConsentChannel.WhatsApp,
            OutreachChannel.LinkedIn => ConsentChannel.LinkedIn,
            _ => ConsentChannel.Email
        };

        if (!await HasConsentAsync(message.LeadId, consentChannel, cancellationToken))
        {
            throw new InvalidOperationException("Candidate has not consented to contact via this channel.");
        }

        switch (message.Channel)
        {
            case OutreachChannel.Email:
                var recipients = message.Lead.Emails
                    .Where(e => !string.IsNullOrWhiteSpace(e.Email))
                    .Select(e => e.Email)
                    .ToList();

                if (recipients.Count == 0 && !string.IsNullOrWhiteSpace(message.Lead.Email))
                {
                    recipients.Add(message.Lead.Email);
                }

                if (recipients.Count == 0)
                {
                    throw new InvalidOperationException("Lead has no email address.");
                }

                var trackedBody = _emailTracking.RewriteLinks(message.Body, message.Id);
                var emailBody = _unsubscribeService.AppendFooter(trackedBody, message.Lead);
                emailBody += _emailTracking.BuildOpenPixel(message.Id);
                await _emailService.SendAsync(
                    recipients,
                    message.Subject ?? "(no subject)",
                    emailBody,
                    _unsubscribeService.BuildHeaders(message.Lead),
                    cancellationToken,
                    ResolveSender(message));
                break;

            case OutreachChannel.LinkedIn:
                if (string.IsNullOrWhiteSpace(message.Lead.LinkedInUrl))
                {
                    throw new InvalidOperationException("Lead has no LinkedIn URL.");
                }

                var plainBody = _emailTracking.RewriteLinksInPlainText(HtmlToPlainText.Convert(message.Body), message.Id);
                var result = await _linkedInService.SendInMailAsync(
                    message.Lead.LinkedInUrl, message.Subject ?? string.Empty, plainBody, cancellationToken);

                if (!result.Sent)
                {
                    return new MessageSendResult(false, result.Message);
                }
                break;

            case OutreachChannel.Sms:
                var phone = message.Lead.Phone;
                if (string.IsNullOrWhiteSpace(phone))
                {
                    throw new InvalidOperationException("Lead has no phone number.");
                }

                var smsText = HtmlToPlainText.Convert(message.Body);
                var smsResult = await _smsService.SendAsync(phone, smsText, cancellationToken);
                if (!smsResult.Sent)
                {
                    throw new InvalidOperationException($"SMS/RCS send failed: {smsResult.Error}");
                }
                break;

            default:
                throw new InvalidOperationException("Sending via this channel is not yet supported.");
        }

        message.Status = OutreachMessageStatus.Sent;
        message.SentAt = DateTime.UtcNow;
        message.ErrorMessage = null;

        if (message.Lead.Status == LeadStatus.New)
        {
            message.Lead.Status = LeadStatus.Contacted;
            message.Lead.UpdatedAt = DateTime.UtcNow;
        }

        return new MessageSendResult(true, null);
    }

    public async Task<CampaignRunResult> RunCampaignAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _dbContext.Campaigns
            .Include(c => c.Sequence)
                .ThenInclude(s => s!.Steps)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            return new CampaignRunResult(0, 0, 0, 0);
        }

        try
        {
            if (campaign.Sequence is null || campaign.Sequence.Steps.Count == 0)
            {
                throw new InvalidOperationException("Add a sequence with at least one step before running this campaign.");
            }

            var steps = campaign.Sequence.Steps.OrderBy(s => s.Order).ToList();
            var now = DateTime.UtcNow;
            var sent = 0;
            var failed = 0;
            var skipped = 0;
            var advanced = 0;

            var messages = await _dbContext.OutreachMessages
                .Where(m => m.CampaignId == campaignId)
                .Include(m => m.Lead)
                    .ThenInclude(l => l.Emails)
                .ToListAsync(cancellationToken);

            var leadsNeedingEmails = messages
                .Where(m => m.Channel == OutreachChannel.Email && m.Status != OutreachMessageStatus.Sent)
                .Select(m => m.Lead)
                .Where(l => l.Status != LeadStatus.OptedOut)
                .Distinct()
                .ToList();

            await FetchAndStoreLeadEmailsAsync(leadsNeedingEmails, cancellationToken);

            foreach (var group in messages.GroupBy(m => m.LeadId).Select(g => g.ToList()))
            {
                var lead = group.First().Lead;
                if (lead.Status == LeadStatus.OptedOut)
                {
                    continue;
                }

                var order = 0;
                var guard = 0;
                while (order < steps.Count && guard++ < steps.Count + 1)
                {
                    var step = steps[order];
                    var stepNumber = order + 1;

                    var alreadySentAtStep = group.Any(m => m.StepOrder == stepNumber && m.Status == OutreachMessageStatus.Sent);
                    if (alreadySentAtStep)
                    {
                        order++;
                        continue;
                    }

                    if (order > 0)
                    {
                        var previousSent = group
                            .Where(m => m.StepOrder == order && m.Status == OutreachMessageStatus.Sent && m.SentAt is not null)
                            .OrderByDescending(m => m.SentAt)
                            .FirstOrDefault();

                        if (previousSent is null)
                        {
                            break;
                        }

                        if (DateTime.UtcNow < previousSent.SentAt!.Value.AddDays(Math.Max(0, step.DelayDays)))
                        {
                            break;
                        }

                        if (!ConditionSatisfied(step.Condition, previousSent))
                        {
                            break;
                        }
                    }

                    var pending = group
                        .Where(m => m.Status == OutreachMessageStatus.Draft || m.Status == OutreachMessageStatus.Failed)
                        .ToList();

                    var stale = pending.Where(m => m.StepOrder != stepNumber).ToList();
                    if (stale.Count > 0)
                    {
                        _dbContext.OutreachMessages.RemoveRange(stale);
                    }

                    var pendingAtStep = pending.FirstOrDefault(m => m.StepOrder == stepNumber);
                    SendOutcome outcome;
                    if (pendingAtStep is not null)
                    {
                        outcome = await TrySendAsync(pendingAtStep, cancellationToken);
                    }
                    else
                    {
                        var draft = BuildDraft(lead, campaignId, step.SubjectTemplate ?? string.Empty, step.BodyTemplate, step.Channel, stepNumber);
                        _dbContext.OutreachMessages.Add(draft);
                        group.Add(draft);
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        advanced++;
                        outcome = await TrySendAsync(draft, cancellationToken);
                    }

                    switch (outcome)
                    {
                        case SendOutcome.Sent: sent++; break;
                        case SendOutcome.Failed: failed++; break;
                        default: skipped++; break;
                    }

                    if (outcome != SendOutcome.Sent)
                    {
                        break;
                    }

                    order++;
                }
            }

            campaign.Status = CampaignStatus.Active;
            campaign.StartedAt ??= now;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CampaignRunResult(sent, failed, skipped, advanced);
        }
        finally
        {
            campaign.IsRunning = false;
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    public async Task<int> RegenerateDraftsAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _dbContext.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
        if (campaign is null || campaign.SequenceId is null)
        {
            return 0;
        }

        var firstStep = await _dbContext.SequenceSteps
            .Where(s => s.SequenceId == campaign.SequenceId.Value)
            .OrderBy(s => s.Order)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (firstStep is null)
        {
            return 0;
        }

        var pending = await _dbContext.OutreachMessages
            .Where(m => m.CampaignId == campaignId && (m.Status == OutreachMessageStatus.Draft || m.Status == OutreachMessageStatus.Failed))
            .Include(m => m.Lead)
            .ToListAsync(cancellationToken);

        var contactedLeadIds = await _dbContext.OutreachMessages
            .Where(m => m.CampaignId == campaignId && m.Status == OutreachMessageStatus.Sent && m.StepOrder != null)
            .Select(m => m.LeadId)
            .ToListAsync(cancellationToken);
        var contactedLeadIdSet = contactedLeadIds.ToHashSet();

        var toRemove = pending.Where(m => !contactedLeadIdSet.Contains(m.LeadId)).ToList();
        if (toRemove.Count == 0)
        {
            return 0;
        }

        _dbContext.OutreachMessages.RemoveRange(toRemove);

        var leads = toRemove.Select(m => m.Lead).Distinct().ToList();
        var created = leads
            .Select(lead => BuildDraft(
                lead,
                campaignId,
                firstStep.SubjectTemplate ?? string.Empty,
                firstStep.BodyTemplate,
                firstStep.Channel,
                firstStep.Order + 1))
            .ToList();

        _dbContext.OutreachMessages.AddRange(created);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return created.Count;
    }

    public async Task<int> RestartAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
        if (campaign is null || campaign.SequenceId is null)
        {
            return 0;
        }

        var firstStep = await _dbContext.SequenceSteps
            .Where(s => s.SequenceId == campaign.SequenceId.Value)
            .OrderBy(s => s.Order)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (firstStep is null)
        {
            return 0;
        }

        var leadIds = await _dbContext.OutreachMessages
            .Where(m => m.CampaignId == campaignId)
            .Select(m => m.LeadId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (leadIds.Count == 0)
        {
            return 0;
        }

        var messages = await _dbContext.OutreachMessages
            .Where(m => m.CampaignId == campaignId)
            .ToListAsync(cancellationToken);

        _dbContext.OutreachMessages.RemoveRange(messages);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var leads = await _dbContext.Leads
            .Where(l => leadIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        var drafts = leads
            .Where(l => l.Status != LeadStatus.OptedOut)
            .Select(lead => BuildDraft(
                lead,
                campaignId,
                firstStep.SubjectTemplate ?? string.Empty,
                firstStep.BodyTemplate,
                firstStep.Channel,
                firstStep.Order + 1))
            .ToList();

        _dbContext.OutreachMessages.AddRange(drafts);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return drafts.Count;
    }

    private enum SendOutcome { Sent, Failed, Skipped }

    private async Task FetchAndStoreLeadEmailsAsync(IReadOnlyCollection<Lead> leads, CancellationToken cancellationToken)
    {
        var toFetch = leads
            .Where(l => !string.IsNullOrWhiteSpace(l.ExternalId))
            .Where(l => !l.Emails.Any(e => !string.IsNullOrWhiteSpace(e.Email)))
            .ToList();

        if (toFetch.Count == 0)
        {
            return;
        }

        var profileIds = toFetch.Select(l => l.ExternalId!).Distinct().ToList();

        IReadOnlyDictionary<string, RhetorikContactEmailData> fetched;
        try
        {
            fetched = await _rhetorikClient.FetchContactEmailsAsync(profileIds, cancellationToken);
        }
        catch
        {
            return;
        }

        foreach (var lead in toFetch)
        {
            if (!fetched.TryGetValue(lead.ExternalId!, out var data))
            {
                continue;
            }

            var eligible = RhetorikEmailSelector.SelectEligible(data.ContactEmails, data.ProfileEmails);
            if (eligible.Count == 0)
            {
                continue;
            }

            var emails = eligible
                .Select((entry, index) => new LeadEmail
                {
                    Email = entry.Address,
                    Type = entry.Type,
                    IsVerified = entry.IsVerified,
                    IsPrimary = index == 0,
                    Priority = index + 1
                })
                .ToList();

            foreach (var email in emails)
            {
                lead.Emails.Add(email);
            }

            if (string.IsNullOrWhiteSpace(lead.Email))
            {
                lead.Email = emails[0].Email;
            }

            lead.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SendOutcome> TrySendAsync(OutreachMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var result = await SendMessageAsync(message, cancellationToken);
            return result.Sent ? SendOutcome.Sent : SendOutcome.Skipped;
        }
        catch (Exception ex)
        {
            message.Status = OutreachMessageStatus.Failed;
            message.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            return SendOutcome.Failed;
        }
    }

    private static bool ConditionSatisfied(SequenceStepCondition condition, OutreachMessage previous) => condition switch
    {
        SequenceStepCondition.IfNotOpened => previous.OpenedAt is null,
        SequenceStepCondition.IfNotClicked => previous.ClickedAt is null,
        SequenceStepCondition.IfNotReplied => previous.RepliedAt is null,
        _ => true
    };

    private async Task<bool> HasConsentAsync(int leadId, ConsentChannel channel, CancellationToken cancellationToken)
    {
        var consent = await _dbContext.ChannelConsents
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LeadId == leadId && c.Channel == channel, cancellationToken);

        if (consent is null)
        {
            return channel == ConsentChannel.Email || channel == ConsentChannel.LinkedIn;
        }

        return consent.Status switch
        {
            ConsentStatus.DoNotContact => false,
            ConsentStatus.OptedOut => false,
            ConsentStatus.OptedIn => true,
            _ => channel == ConsentChannel.Email || channel == ConsentChannel.LinkedIn
        };
    }

    private OutreachMessage BuildDraft(Lead lead, int campaignId, string subjectTemplate, string bodyTemplate, OutreachChannel channel, int? stepOrder)
    {
        var orgName = _dbContext.OrganizationProfiles.AsNoTracking().FirstOrDefault()?.OrgName;
        var sender = _senderProvider.Current;

        return new OutreachMessage
        {
            LeadId = lead.Id,
            CampaignId = campaignId,
            Channel = channel,
            Subject = _personalization.RenderTemplate(subjectTemplate, lead, orgName: orgName),
            Body = _personalization.RenderTemplate(bodyTemplate, lead, orgName: orgName),
            StepOrder = stepOrder,
            Status = OutreachMessageStatus.Draft,
            SentByUserId = sender.UserId,
            FromAddress = sender.FromAddress,
            FromName = sender.FromName,
            ReplyTo = sender.ReplyTo
        };
    }

    private SenderIdentity ResolveSender(OutreachMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.FromAddress) ||
            !string.IsNullOrWhiteSpace(message.FromName) ||
            !string.IsNullOrWhiteSpace(message.ReplyTo))
        {
            return new SenderIdentity(message.SentByUserId, message.FromAddress, message.FromName, message.ReplyTo);
        }

        return _senderProvider.Current;
    }
}

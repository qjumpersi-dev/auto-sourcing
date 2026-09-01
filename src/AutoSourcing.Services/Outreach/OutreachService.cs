using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Services.Outreach;

public class OutreachService : IOutreachService
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IPersonalizationService _personalization;

    public OutreachService(AutoSourcingDbContext dbContext, IPersonalizationService personalization)
    {
        _dbContext = dbContext;
        _personalization = personalization;
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

        var message = BuildDraft(lead, campaignId, subjectTemplate, bodyTemplate, channel);

        _dbContext.OutreachMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task<List<OutreachMessage>> AddLeadsToCampaignAsync(int campaignId, IReadOnlyCollection<int> leadIds, CancellationToken cancellationToken = default)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

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

            created.Add(BuildDraft(lead, campaignId, campaign.SubjectTemplate ?? string.Empty, campaign.BodyTemplate ?? string.Empty, campaign.Channel));
        }

        if (created.Count > 0)
        {
            _dbContext.OutreachMessages.AddRange(created);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    private OutreachMessage BuildDraft(Lead lead, int campaignId, string subjectTemplate, string bodyTemplate, OutreachChannel channel) => new()
    {
        LeadId = lead.Id,
        CampaignId = campaignId,
        Channel = channel,
        Subject = _personalization.RenderTemplate(subjectTemplate, lead),
        Body = _personalization.RenderTemplate(bodyTemplate, lead),
        Status = OutreachMessageStatus.Draft
    };
}

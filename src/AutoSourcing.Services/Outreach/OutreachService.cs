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

    public async Task<OutreachMessage> CreateDraftAsync(int leadId, int campaignId, string subjectTemplate, string bodyTemplate, CancellationToken cancellationToken = default)
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

        var message = new OutreachMessage
        {
            LeadId = lead.Id,
            CampaignId = campaignId,
            Channel = OutreachChannel.Email,
            Subject = _personalization.RenderTemplate(subjectTemplate, lead),
            Body = _personalization.RenderTemplate(bodyTemplate, lead),
            Status = OutreachMessageStatus.Draft
        };

        _dbContext.OutreachMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return message;
    }
}

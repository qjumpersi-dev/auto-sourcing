using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Services.Outreach;

public interface IPersonalizationService
{
    string RenderTemplate(string template, Lead lead);
}

public interface IOutreachService
{
    Task<OutreachMessage> CreateDraftAsync(int leadId, int campaignId, string subjectTemplate, string bodyTemplate, OutreachChannel channel, CancellationToken cancellationToken = default);
    Task<List<OutreachMessage>> AddLeadsToCampaignAsync(int campaignId, IReadOnlyCollection<int> leadIds, CancellationToken cancellationToken = default);
}

using AutoSourcing.Core.Entities;

namespace AutoSourcing.Services.Outreach;

public interface IPersonalizationService
{
    string RenderTemplate(string template, Lead lead);
}

public interface IOutreachService
{
    Task<OutreachMessage> CreateDraftAsync(int leadId, int campaignId, string subjectTemplate, string bodyTemplate, CancellationToken cancellationToken = default);
}

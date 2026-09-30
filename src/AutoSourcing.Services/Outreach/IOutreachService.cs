using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Services.Outreach;

public record PersonalisationOption(string Key, string Name, string Description, string Template);

public interface IPersonalizationService
{
    string RenderTemplate(string template, Lead lead, string? jobUrl = null, string? orgName = null, string? jobLocation = null);
    IReadOnlyList<PersonalisationOption> GetOptions();
}

public record MessageSendResult(bool Sent, string? Message, bool Deferred = false);

public record CampaignRunResult(int Sent, int Failed, int Skipped, int Advanced, int Deferred = 0);

public interface IOutreachService
{
    Task<OutreachMessage> CreateDraftAsync(int leadId, int campaignId, string subjectTemplate, string bodyTemplate, OutreachChannel channel, CancellationToken cancellationToken = default);
    Task<List<OutreachMessage>> AddLeadsToCampaignAsync(int campaignId, IReadOnlyCollection<int> leadIds, CancellationToken cancellationToken = default);
    Task<MessageSendResult> SendMessageAsync(OutreachMessage message, CancellationToken cancellationToken = default);
    Task<CampaignRunResult> RunCampaignAsync(int campaignId, CancellationToken cancellationToken = default);
    Task<int> RegenerateDraftsAsync(int campaignId, CancellationToken cancellationToken = default);
    Task<int> RestartAsync(int campaignId, CancellationToken cancellationToken = default);
}

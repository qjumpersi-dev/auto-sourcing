using AutoSourcing.Services.Outreach;

namespace AutoSourcing.API.BackgroundServices;

public class CampaignRunBackgroundService : BackgroundService
{
    private readonly ICampaignRunQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignRunBackgroundService> _logger;

    public CampaignRunBackgroundService(
        ICampaignRunQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<CampaignRunBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var campaignId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var outreachService = scope.ServiceProvider.GetRequiredService<IOutreachService>();
                var result = await outreachService.RunCampaignAsync(campaignId, stoppingToken);

                _logger.LogInformation(
                    "Campaign {CampaignId} run finished: sent {Sent}, failed {Failed}, skipped {Skipped}, advanced {Advanced}",
                    campaignId, result.Sent, result.Failed, result.Skipped, result.Advanced);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Campaign {CampaignId} run failed", campaignId);
            }
        }
    }
}

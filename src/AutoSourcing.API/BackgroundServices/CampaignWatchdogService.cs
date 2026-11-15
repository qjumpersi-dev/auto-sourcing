using AutoSourcing.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.BackgroundServices;

// Clears "running" flags left behind by runs that were lost or hung, so the UI doesn't show a
// campaign as permanently running.
public class CampaignWatchdogService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignWatchdogService> _logger;

    public CampaignWatchdogService(IServiceScopeFactory scopeFactory, ILogger<CampaignWatchdogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AutoSourcingDbContext>();
                var cutoff = DateTime.UtcNow - StaleAfter;

                var stale = await dbContext.Campaigns
                    .Where(c => c.IsRunning && (c.LastRunAt == null || c.LastRunAt < cutoff))
                    .ToListAsync(stoppingToken);

                if (stale.Count > 0)
                {
                    foreach (var campaign in stale)
                    {
                        campaign.IsRunning = false;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Cleared stale running flag on {Count} campaign(s).", stale.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Campaign watchdog sweep failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
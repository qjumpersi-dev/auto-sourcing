using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Interviews;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.BackgroundServices;

// Once an interview has finished, pull the Teams transcript and summarise it. Runs quietly in
// the background and simply retries later if the transcript is not ready yet.
public class InterviewTranscriptBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InterviewTranscriptBackgroundService> _logger;

    public InterviewTranscriptBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<InterviewTranscriptBackgroundService> logger)
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
                var interviewService = scope.ServiceProvider.GetRequiredService<IInterviewService>();

                // Give the meeting time to finish before looking for its transcript.
                var cutoff = DateTime.UtcNow.AddMinutes(-45);

                var pending = await dbContext.Interviews
                    .IgnoreQueryFilters()
                    .Where(i => i.Status == InterviewStatus.Booked && i.Transcript == null && i.StartAt < cutoff)
                    .OrderBy(i => i.StartAt)
                    .Select(i => i.Id)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var id in pending)
                {
                    var processed = await interviewService.ProcessTranscriptAsync(id, stoppingToken);
                    _logger.LogInformation("Interview {InterviewId} transcript processed: {Processed}", id, processed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Interview transcript sweep failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}

using System.Net.Http.Json;
using AutoSourcing.Services.LinkedIn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

if (args.Contains("--install"))
{
    Console.WriteLine("Installing the Playwright Chromium browser...");
    Microsoft.Playwright.Program.Main(["install", "chromium"]);
    Console.WriteLine("Done.");
    return;
}

var apiUrl = Environment.GetEnvironmentVariable("AITS_API_URL") ?? "http://localhost:5000";
var apiKey = Environment.GetEnvironmentVariable("AITS_API_KEY") ?? string.Empty;
var userDataDir = Environment.GetEnvironmentVariable("LINKEDIN_USER_DATA_DIR")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoSourcing", "LinkedInProfile");
var headless = string.Equals(Environment.GetEnvironmentVariable("LINKEDIN_HEADLESS"), "true", StringComparison.OrdinalIgnoreCase);
var intervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("WORKER_INTERVAL_SECONDS"), out var parsedInterval)
    ? Math.Max(5, parsedInterval)
    : 30;

var options = Options.Create(new LinkedInOptions
{
    Headless = headless,
    UserDataDir = userDataDir
});

var linkedIn = new PlaywrightLinkedInService(options, new ConsoleLogger<PlaywrightLinkedInService>());

using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };
if (!string.IsNullOrWhiteSpace(apiKey))
{
    http.DefaultRequestHeaders.Add("X-API-Key", apiKey);
}

Console.WriteLine("AITS LinkedIn worker");
Console.WriteLine($"  API:         {apiUrl}");
Console.WriteLine($"  Profile dir: {userDataDir}");
Console.WriteLine($"  Poll every:  {intervalSeconds}s");
Console.WriteLine();

try
{
    if (!await linkedIn.IsSignedInAsync())
    {
        Console.WriteLine("Not signed in to LinkedIn. A browser window will open - log in, then the worker continues.");
        await linkedIn.SignInAsync();
    }

    Console.WriteLine("Signed in to LinkedIn. Watching the queue...");
}
catch (Exception ex)
{
    Console.WriteLine($"Could not start the LinkedIn browser: {ex.Message}");
    Console.WriteLine("If the browser is missing, run: dotnet run --project tools/LinkedInWorker -- --install");
    return;
}

while (true)
{
    try
    {
        var queue = await http.GetFromJsonAsync<List<QueueItem>>("/api/linkedin/queue");
        if (queue is { Count: > 0 })
        {
            Console.WriteLine($"{queue.Count} queued InMail(s) found.");
            foreach (var item in queue)
            {
                if (string.IsNullOrWhiteSpace(item.ProfileUrl))
                {
                    await ReportAsync(http, item.MessageId, false, "Lead has no LinkedIn URL.");
                    Console.WriteLine($"  msg {item.MessageId} ({item.CandidateName}): no LinkedIn URL");
                    continue;
                }

                try
                {
                    var result = await linkedIn.SendInMailAsync(item.ProfileUrl, item.Subject ?? string.Empty, item.Body);
                    await ReportAsync(http, item.MessageId, result.Sent, result.Message);
                    Console.WriteLine($"  msg {item.MessageId} ({item.CandidateName}): {(result.Sent ? "sent" : result.Message)}");
                }
                catch (Exception ex)
                {
                    await ReportAsync(http, item.MessageId, false, ex.Message);
                    Console.WriteLine($"  msg {item.MessageId} ({item.CandidateName}): error - {ex.Message}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Worker error: {ex.Message}");
    }

    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
}

static async Task ReportAsync(HttpClient http, int messageId, bool sent, string? error)
{
    await http.PostAsJsonAsync($"/api/linkedin/queue/{messageId}", new { sent, error });
}

internal sealed record QueueItem(
    int MessageId,
    int CampaignId,
    int LeadId,
    string CandidateName,
    string? ProfileUrl,
    string? Subject,
    string Body);

internal sealed class ConsoleLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }
}

using System.Net;
using System.Text.Json;
using AutoSourcing.Data;
using AutoSourcing.Services.Email;
using Microsoft.AspNetCore.Mvc;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    private static readonly byte[] TransparentGif = Convert.FromBase64String(
        "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    private readonly AutoSourcingDbContext _dbContext;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(AutoSourcingDbContext dbContext, ILogger<TrackingController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Automated fetchers that follow links in email. Their requests must not count as clicks.
    private static readonly string[] ScannerMarkers =
    [
        "bot", "crawler", "crawl", "spider", "scan", "preview", "proofpoint", "mimecast",
        "barracuda", "symantec", "forcepoint", "safelinks", "headless", "python", "curl/",
        "wget", "axios", "node-fetch", "go-http", "java/", "okhttp", "libwww", "httpclient"
    ];

    private bool LooksAutomated()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return true;
        }

        var lower = userAgent.ToLowerInvariant();
        return ScannerMarkers.Any(marker => lower.Contains(marker));
    }

    [HttpGet("open/{messageId:int}.gif")]
    public async Task<IActionResult> TrackOpen(int messageId, CancellationToken cancellationToken)
    {
        var message = await _dbContext.OutreachMessages.FindAsync([messageId], cancellationToken);
        if (message is not null && message.OpenedAt is null)
        {
            message.OpenedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers.Pragma = "no-cache";
        return File(TransparentGif, "image/gif");
    }

    // A click is only recorded when a real browser renders this page and runs the beacon.
    // Link scanners prefetch URLs but don't run JavaScript, so they no longer create false clicks.
    [HttpGet("go/{messageId:int}/{token}")]
    public IActionResult Go(int messageId, string token)
    {
        var url = UrlToken.Decode(token);
        return RedirectPage(messageId, url);
    }

    // Kept so links in already-sent emails behave the same way.
    [HttpGet("click/{messageId:int}/{token}")]
    public IActionResult TrackClickToken(int messageId, string token)
    {
        var url = UrlToken.Decode(token);
        return RedirectPage(messageId, url);
    }

    [HttpGet("click/{messageId:int}")]
    public IActionResult TrackClick(int messageId, [FromQuery] string? url) => RedirectPage(messageId, url);

    // Called by the redirect page's JavaScript only.
    [HttpPost("confirm/{messageId:int}")]
    public async Task<IActionResult> ConfirmClick(int messageId, CancellationToken cancellationToken)
    {
        var userAgent = Request.Headers.UserAgent.ToString();

        if (LooksAutomated())
        {
            _logger.LogInformation(
                "Ignored tracking click for message {MessageId} (automated fetcher). UA: {UserAgent}",
                messageId,
                userAgent);
            return NoContent();
        }

        var message = await _dbContext.OutreachMessages.FindAsync([messageId], cancellationToken);

        // Scanners follow links within seconds of delivery; people don't.
        if (message?.SentAt is { } sentAt && DateTime.UtcNow - sentAt < TimeSpan.FromSeconds(60))
        {
            _logger.LogInformation(
                "Ignored tracking click for message {MessageId} (within 60s of send). UA: {UserAgent}",
                messageId,
                userAgent);
            return NoContent();
        }

        if (message is not null && message.ClickedAt is null)
        {
            message.ClickedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Recorded tracking click for message {MessageId}. UA: {UserAgent}",
                messageId,
                userAgent);
        }

        return NoContent();
    }

    private IActionResult RedirectPage(int messageId, string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var target))
        {
            return BadRequest();
        }

        var href = WebUtility.HtmlEncode(target.AbsoluteUri);
        var jsonUrl = JsonSerializer.Serialize(target.AbsoluteUri);

        var html =
            "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\" />" +
            "<meta name=\"robots\" content=\"noindex\" /><title>Redirecting…</title></head>" +
            "<body style=\"font-family:system-ui,-apple-system,sans-serif;padding:2rem;color:#6b7280;\">" +
            "<p>Taking you to the link…</p>" +
            $"<p><a href=\"{href}\">Continue</a></p>" +
            "<script>(function(){" +
            // Only a real browser counts: headless fetchers report webdriver/odd fingerprints.
            "var real=false;try{real=!navigator.webdriver&&navigator.languages&&navigator.languages.length>0" +
            "&&(navigator.hardwareConcurrency||0)>0&&navigator.plugins&&navigator.plugins.length>0;}catch(e){real=false;}" +
            $"if(real){{try{{fetch('/api/tracking/confirm/{messageId}',{{method:'POST',keepalive:true}});}}catch(e){{}}}}" +
            $"location.replace({jsonUrl});" +
            "})();</script>" +
            "</body></html>";

        return Content(html, "text/html");
    }
}

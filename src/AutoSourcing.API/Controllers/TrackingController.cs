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

    public TrackingController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
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
        var message = await _dbContext.OutreachMessages.FindAsync([messageId], cancellationToken);
        if (message is not null && message.ClickedAt is null)
        {
            message.ClickedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
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
            $"try{{fetch('/api/tracking/confirm/{messageId}',{{method:'POST',keepalive:true}});}}catch(e){{}}" +
            $"location.replace({jsonUrl});" +
            "})();</script>" +
            "</body></html>";

        return Content(html, "text/html");
    }
}

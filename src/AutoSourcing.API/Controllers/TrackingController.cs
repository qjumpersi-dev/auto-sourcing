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

    [HttpGet("click/{messageId:int}/{token}")]
    public async Task<IActionResult> TrackClickToken(int messageId, string token, CancellationToken cancellationToken)
    {
        await RecordClickAsync(messageId, cancellationToken);

        var url = UrlToken.Decode(token);
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var target))
        {
            return BadRequest();
        }

        return Redirect(target.AbsoluteUri);
    }

    [HttpGet("click/{messageId:int}")]
    public async Task<IActionResult> TrackClick(int messageId, [FromQuery] string? url, CancellationToken cancellationToken)
    {
        await RecordClickAsync(messageId, cancellationToken);

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var target))
        {
            return BadRequest();
        }

        return Redirect(target.AbsoluteUri);
    }

    private async Task RecordClickAsync(int messageId, CancellationToken cancellationToken)
    {
        var message = await _dbContext.OutreachMessages.FindAsync([messageId], cancellationToken);
        if (message is not null && message.ClickedAt is null)
        {
            message.ClickedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/unsubscribe")]
public class UnsubscribeController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;

    public UnsubscribeController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{leadId:int}")]
    [HttpPost("{leadId:int}")]
    public async Task<ContentResult> Unsubscribe(int leadId, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken);
        if (lead is not null && lead.Status != LeadStatus.OptedOut)
        {
            lead.Status = LeadStatus.OptedOut;
            lead.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        const string html = """
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8" /><title>Unsubscribed</title></head>
            <body style="font-family:system-ui,-apple-system,sans-serif;padding:3rem;color:#111827;">
              <h1 style="font-size:1.25rem;">You have been unsubscribed</h1>
              <p style="color:#6b7280;">You will no longer receive outreach emails from us.</p>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }
}

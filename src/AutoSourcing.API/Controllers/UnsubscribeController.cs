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

    // GET must NOT change state: email clients and security scanners prefetch links,
    // which would otherwise opt people out without them clicking anything.
    [HttpGet("{leadId:int}")]
    public async Task<ContentResult> Confirm(int leadId, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken);

        if (lead is null)
        {
            return Content(Page("Unsubscribe", "<p>This unsubscribe link is not valid.</p>"), "text/html");
        }

        if (lead.Status == LeadStatus.OptedOut)
        {
            return Content(
                Page("Already unsubscribed", "<p>You have already been unsubscribed. You will no longer receive outreach emails from us.</p>"),
                "text/html");
        }

        var form =
            "<p>Click the button below to stop receiving outreach emails from us.</p>" +
            $"<form method=\"post\" action=\"/api/unsubscribe/{leadId}\">" +
            "<button type=\"submit\" style=\"background:#2563eb;color:#fff;border:0;border-radius:6px;padding:10px 18px;font-size:15px;cursor:pointer;\">Unsubscribe</button>" +
            "</form>";

        return Content(Page("Unsubscribe", form), "text/html");
    }

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

        return Content(
            Page("Unsubscribed", "<p>You have been unsubscribed. You will no longer receive outreach emails from us.</p>"),
            "text/html");
    }

    private static string Page(string title, string body) =>
        "<!doctype html>" +
        "<html lang=\"en\">" +
        "<head><meta charset=\"utf-8\" /><meta name=\"robots\" content=\"noindex\" />" +
        $"<title>{title}</title></head>" +
        "<body style=\"font-family:system-ui,-apple-system,sans-serif;padding:3rem;color:#111827;\">" +
        $"<h1 style=\"font-size:1.25rem;\">{title}</h1>" +
        "<div style=\"color:#6b7280;\">" + body + "</div>" +
        "</body></html>";
}

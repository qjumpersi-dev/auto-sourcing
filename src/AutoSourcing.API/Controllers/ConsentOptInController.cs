using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/consent")]
public class ConsentOptInController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;

    public ConsentOptInController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{leadId:int}")]
    public async Task<ContentResult> ShowOptInForm(int leadId, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken);
        var phone = lead?.Phone ?? "";

        var html = $$"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Sign up for SMS updates</title></head>
            <body style="font-family:system-ui,-apple-system,sans-serif;max-width:480px;margin:2rem auto;padding:0 1rem;color:#111827;">
              <h1 style="font-size:1.25rem;">Sign up for SMS updates from QJumpers</h1>
              <form method="POST" action="/api/consent/{{leadId}}">
                <div style="margin-bottom:1rem;">
                  <label style="display:block;font-weight:600;margin-bottom:.25rem;">Mobile phone number</label>
                  <input type="tel" name="phone" value="{{phone}}" placeholder="(555) 123-4567" required style="width:100%;padding:.5rem;border:1px solid #d1d5db;border-radius:.375rem;font-size:1rem;" />
                </div>
                <div style="margin-bottom:1rem;display:flex;gap:.5rem;align-items:flex-start;">
                  <input type="checkbox" id="consent" name="consent" required style="margin-top:.25rem;" />
                  <label for="consent" style="font-size:.875rem;">Yes, I would like to receive automated text messages from <strong>QJumpers US C Corp</strong> about job opportunities, interview scheduling, interview reminders, and recruitment updates. I understand I will receive up to 1 message per month for job opportunities, and up to 4 messages per month if I apply for a job.</label>
                </div>
                <div style="margin-bottom:1rem;font-size:.875rem;line-height:1.5;">
                  <p><strong>Message Frequency:</strong> You will receive up to 1 message per month for job opportunities, and up to 4 messages per month if you apply for a job.</p>
                  <p><strong>Standard Rates:</strong> Message and data rates may apply depending on your mobile phone service plan.</p>
                  <p><strong>Help &amp; Stop:</strong> Reply HELP for help or STOP to cancel at any time.</p>
                  <p>By providing your phone number and checking the box above, you agree to receive text messages from QJumpers US C Corp. Consent is not required to make a purchase.</p>
                </div>
                <p style="font-size:.875rem;margin-bottom:1rem;">
                  <a href="https://employer.qjumpers.com/AISourcingterms" style="color:#2563eb;">Terms of Service</a> |
                  <a href="https://qjumpers.com/us/privacy-policy/" style="color:#2563eb;">Privacy Policy</a>
                </p>
                <button type="submit" style="width:100%;padding:.75rem;background:#000;color:#fff;border:none;border-radius:.375rem;font-size:1rem;font-weight:600;cursor:pointer;">Yes, sign me up!</button>
              </form>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }

    [HttpPost("{leadId:int}")]
    public async Task<ContentResult> OptIn(int leadId, [FromForm] string? phone, [FromForm] string? consent, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken);

        if (lead is not null && consent == "on")
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                lead.Phone = phone;
            }

            var channels = new[] { ConsentChannel.Email, ConsentChannel.Sms, ConsentChannel.LinkedIn };
            foreach (var channel in channels)
            {
                var existing = await _dbContext.ChannelConsents
                    .FirstOrDefaultAsync(c => c.LeadId == leadId && c.Channel == channel, cancellationToken);

                if (existing is null)
                {
                    _dbContext.ChannelConsents.Add(new ChannelConsent
                    {
                        LeadId = leadId,
                        Channel = channel,
                        Status = ConsentStatus.OptedIn,
                        OptInSource = "A2P consent form",
                        OptInDate = DateTime.UtcNow
                    });
                }
                else if (existing.Status != ConsentStatus.OptedIn)
                {
                    existing.Status = ConsentStatus.OptedIn;
                    existing.OptInSource = "A2P consent form";
                    existing.OptInDate = DateTime.UtcNow;
                    existing.OptOutDate = null;
                }
            }

            lead.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var html = """
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8" /><title>Thank you</title></head>
            <body style="font-family:system-ui,-apple-system,sans-serif;padding:3rem;color:#111827;text-align:center;">
              <h1 style="font-size:1.5rem;">You're signed up!</h1>
              <p style="color:#6b7280;max-width:400px;margin:1rem auto;">
                You'll now receive text messages from QJumpers about job opportunities and recruitment updates.
                Reply STOP to cancel at any time.
              </p>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }
}

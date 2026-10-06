using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

// Twilio callbacks. This path is public (no API key) so Twilio can reach it.
[ApiController]
[Route("api/sms")]
public class SmsController : ControllerBase
{
    private static readonly string[] OptOutKeywords =
        ["STOP", "STOPALL", "UNSUBSCRIBE", "CANCEL", "END", "QUIT", "OPTOUT"];

    private readonly AutoSourcingDbContext _dbContext;
    private readonly ILogger<SmsController> _logger;

    public SmsController(AutoSourcingDbContext dbContext, ILogger<SmsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Twilio delivery-status callback: updates the message if it failed to deliver.
    [HttpPost("status/{messageId:int}")]
    public async Task<IActionResult> Status(
        int messageId,
        [FromForm] string? MessageStatus,
        [FromForm] string? ErrorCode,
        [FromForm] string? ErrorMessage,
        CancellationToken cancellationToken)
    {
        var message = await _dbContext.OutreachMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message is null)
        {
            return Ok();
        }

        var status = (MessageStatus ?? string.Empty).ToLowerInvariant();

        if (status is "failed" or "undelivered")
        {
            message.Status = OutreachMessageStatus.Failed;
            message.ErrorMessage = string.IsNullOrWhiteSpace(ErrorMessage)
                ? $"Twilio could not deliver the message ({status} {ErrorCode})."
                : $"Twilio: {ErrorMessage}";
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SMS message {MessageId} failed: {Status} {Code}", messageId, status, ErrorCode);
        }

        return Ok();
    }

    // Twilio inbound-message webhook. STOP records the opt-out against the candidate.
    [HttpPost("inbound")]
    public async Task<IActionResult> Inbound(
        [FromForm] string? From,
        [FromForm] string? Body,
        CancellationToken cancellationToken)
    {
        var body = (Body ?? string.Empty).Trim().ToUpperInvariant();
        var from = NormalizePhone(From);

        if (string.IsNullOrWhiteSpace(from) || !OptOutKeywords.Contains(body))
        {
            // HELP and everything else is handled by Twilio's built-in replies.
            return Content("<Response/>", "application/xml");
        }

        var leads = await _dbContext.Leads
            .IgnoreQueryFilters()
            .Where(l => l.Phone != null && l.Phone != "")
            .ToListAsync(cancellationToken);

        var lead = leads.FirstOrDefault(l => NormalizePhone(l.Phone) == from);
        if (lead is not null)
        {
            var consent = await _dbContext.ChannelConsents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.LeadId == lead.Id && c.Channel == ConsentChannel.Sms, cancellationToken);

            if (consent is null)
            {
                _dbContext.ChannelConsents.Add(new ChannelConsent
                {
                    UserId = lead.UserId,
                    LeadId = lead.Id,
                    Channel = ConsentChannel.Sms,
                    Status = ConsentStatus.OptedOut,
                    OptInSource = "SMS STOP",
                    OptOutDate = DateTime.UtcNow
                });
            }
            else
            {
                consent.Status = ConsentStatus.OptedOut;
                consent.OptOutDate = DateTime.UtcNow;
            }

            lead.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Candidate {LeadId} opted out of SMS via STOP.", lead.Id);
        }

        return Content("<Response/>", "application/xml");
    }

    private static string NormalizePhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? string.Empty : new string(phone.Where(char.IsDigit).ToArray());
}

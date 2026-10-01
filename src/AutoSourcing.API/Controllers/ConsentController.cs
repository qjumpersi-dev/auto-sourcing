using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class ConsentUpdateRequest
{
    public ConsentChannel Channel { get; set; }
    public ConsentStatus Status { get; set; }
    public string? OptInSource { get; set; }
    public string? Notes { get; set; }
}

public class LeadConsentResponse
{
    public int LeadId { get; set; }
    public ConsentChannel? PreferredChannel { get; set; }
    public string? Country { get; set; }
    public List<ChannelConsentDto> Consents { get; set; } = [];
}

public class ChannelConsentDto
{
    public ConsentChannel Channel { get; set; }
    public ConsentStatus Status { get; set; }
    public string? OptInSource { get; set; }
    public DateTime? OptInDate { get; set; }
    public DateTime? OptOutDate { get; set; }
    public string? Notes { get; set; }
}

[ApiController]
[Route("api/leads/{leadId:int}/consent")]
public class ConsentController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;

    public ConsentController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<LeadConsentResponse>> GetConsent(int leadId, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        var consents = await _dbContext.ChannelConsents.AsNoTracking()
            .Where(c => c.LeadId == leadId)
            .ToListAsync(cancellationToken);

        return Ok(new LeadConsentResponse
        {
            LeadId = lead.Id,
            PreferredChannel = lead.PreferredChannel,
            Country = lead.Country,
            Consents = consents.Select(c => new ChannelConsentDto
            {
                Channel = c.Channel,
                Status = c.Status,
                OptInSource = c.OptInSource,
                OptInDate = c.OptInDate,
                OptOutDate = c.OptOutDate,
                Notes = c.Notes
            }).ToList()
        });
    }

    [HttpPut]
    public async Task<ActionResult<LeadConsentResponse>> UpdateConsent(int leadId, [FromBody] ConsentUpdateRequest request, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        var consent = await _dbContext.ChannelConsents
            .FirstOrDefaultAsync(c => c.LeadId == leadId && c.Channel == request.Channel, cancellationToken);

        if (consent is null)
        {
            consent = new ChannelConsent
            {
                LeadId = leadId,
                Channel = request.Channel,
                Status = request.Status,
                OptInSource = request.OptInSource,
                Notes = request.Notes
            };
            _dbContext.ChannelConsents.Add(consent);
        }
        else
        {
            consent.Status = request.Status;
            consent.OptInSource = request.OptInSource ?? consent.OptInSource;
            consent.Notes = request.Notes ?? consent.Notes;
        }

        if (request.Status == ConsentStatus.OptedIn)
        {
            consent.OptInDate = DateTime.UtcNow;
            consent.OptOutDate = null;
        }
        else if (request.Status == ConsentStatus.OptedOut || request.Status == ConsentStatus.DoNotContact)
        {
            consent.OptOutDate = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetConsent(leadId, cancellationToken);
    }

    [HttpPut("preferred-channel")]
    public async Task<IActionResult> SetPreferredChannel(int leadId, [FromBody] ConsentChannel? channel, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        lead.PreferredChannel = channel;
        lead.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

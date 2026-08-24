using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Rhetorik;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IRhetorikClient _rhetorikClient;

    public LeadsController(AutoSourcingDbContext dbContext, IRhetorikClient rhetorikClient)
    {
        _dbContext = dbContext;
        _rhetorikClient = rhetorikClient;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Lead>>> GetLeads(CancellationToken cancellationToken)
    {
        return Ok(await _dbContext.Leads.AsNoTracking().OrderByDescending(l => l.CreatedAt).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Lead>> GetLead(int id, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPost("search-rhetorik")]
    public async Task<ActionResult<IReadOnlyList<Lead>>> SearchRhetorik([FromBody] RhetorikSearchRequest request, CancellationToken cancellationToken)
    {
        var profiles = await _rhetorikClient.SearchProfilesAsync(request, cancellationToken);
        return Ok(profiles);
    }

    [HttpPost("import")]
    public async Task<ActionResult<IEnumerable<Lead>>> ImportFromRhetorik([FromBody] RhetorikSearchRequest request, CancellationToken cancellationToken)
    {
        var candidates = await _rhetorikClient.SearchAndMapToLeadsAsync(request, cancellationToken);
        var existingExternalIds = await _dbContext.Leads
            .Where(l => l.ExternalId != null)
            .Select(l => l.ExternalId!)
            .ToListAsync(cancellationToken);

        var newLeads = candidates
            .Where(l => string.IsNullOrEmpty(l.ExternalId) || !existingExternalIds.Contains(l.ExternalId))
            .ToList();

        if (newLeads.Count > 0)
        {
            _dbContext.Leads.AddRange(newLeads);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(newLeads);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] LeadStatus status, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FindAsync([id], cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        lead.Status = status;
        lead.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

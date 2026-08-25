using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.NLSearch;
using AutoSourcing.Services.Rhetorik;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class GenerateSearchRequest
{
    public string Text { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IRhetorikClient _rhetorikClient;
    private readonly INLSearchService _nlSearchService;

    public LeadsController(AutoSourcingDbContext dbContext, IRhetorikClient rhetorikClient, INLSearchService nlSearchService)
    {
        _dbContext = dbContext;
        _rhetorikClient = rhetorikClient;
        _nlSearchService = nlSearchService;
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
    public async Task<ActionResult<ProfileSearchResponse>> SearchRhetorik([FromBody] ProfileSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _rhetorikClient.SearchProfilesAsync(request, cancellationToken));
    }

    [HttpPost("generate-search")]
    public async Task<ActionResult<ProfileSearchRequest>> GenerateSearch([FromBody] GenerateSearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Text is required." });
        }

        return Ok(await _nlSearchService.GenerateSearchSpecAsync(request.Text, cancellationToken));
    }

    [HttpPost("import")]
    public async Task<ActionResult<IEnumerable<Lead>>> ImportFromRhetorik([FromBody] ProfileSearchRequest request, CancellationToken cancellationToken)
    {
        var candidates = await _rhetorikClient.SearchAndMapToLeadsAsync(request, cancellationToken);
        var candidateExternalIds = candidates
            .Where(l => !string.IsNullOrEmpty(l.ExternalId))
            .Select(l => l.ExternalId!)
            .ToList();

        var existingByExternalId = await _dbContext.Leads
            .Where(l => l.ExternalId != null && candidateExternalIds.Contains(l.ExternalId))
            .ToDictionaryAsync(l => l.ExternalId!, cancellationToken);

        var newLeads = new List<Lead>();
        var enriched = 0;

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate.ExternalId) ||
                !existingByExternalId.TryGetValue(candidate.ExternalId, out var existing))
            {
                newLeads.Add(candidate);
                continue;
            }

            var changed = false;
            if (!string.IsNullOrWhiteSpace(candidate.Company) && existing.Company != candidate.Company)
            {
                existing.Company = candidate.Company;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(candidate.JobTitle) && existing.JobTitle != candidate.JobTitle)
            {
                existing.JobTitle = candidate.JobTitle;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(candidate.Phone) && existing.Phone != candidate.Phone)
            {
                existing.Phone = candidate.Phone;
                changed = true;
            }
            if (changed)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                enriched++;
            }
        }

        if (newLeads.Count > 0)
        {
            _dbContext.Leads.AddRange(newLeads);
        }

        if (newLeads.Count > 0 || enriched > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { added = newLeads.Count, enriched });
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

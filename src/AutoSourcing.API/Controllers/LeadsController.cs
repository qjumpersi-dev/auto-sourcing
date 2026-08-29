using System.Text.Json.Serialization;
using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.NLSearch;
using AutoSourcing.Services.Outreach;
using AutoSourcing.Services.Rhetorik;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class GenerateSearchRequest
{
    public string Text { get; set; } = string.Empty;
}

public class LeadDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? LinkedInUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public LeadStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<CampaignRef> Campaigns { get; set; } = [];
}

public class PaginatedLeads
{
    public IReadOnlyList<LeadDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class CampaignRef
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public record CampaignMembershipRow(int LeadId, int CampaignId, string CampaignName);

public class RhetorikProfileResultEnriched
{
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("profile_data")]
    public RhetorikProfileData? ProfileData { get; set; }

    [JsonPropertyName("contact_data")]
    public RhetorikContactDataBlock? ContactData { get; set; }

    [JsonPropertyName("lead_id")]
    public int? LeadId { get; set; }

    [JsonPropertyName("campaigns")]
    public List<CampaignRef> Campaigns { get; set; } = [];
}

public class ProfileSearchResponseEnriched
{
    [JsonPropertyName("counts")]
    public RhetorikCounts? Counts { get; set; }

    [JsonPropertyName("results")]
    public IReadOnlyList<RhetorikProfileResultEnriched> Results { get; set; } = [];

    [JsonPropertyName("pagination")]
    public RhetorikPagination? Pagination { get; set; }
}

public class ImportToCampaignRequest
{
    public int CampaignId { get; set; }
    public List<string> ProfileIds { get; set; } = [];
}

public class ImportToCampaignResponse
{
    public int Added { get; set; }
    public int Skipped { get; set; }
}

public class UpdateLeadStatusRequest
{
    public LeadStatus Status { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IRhetorikClient _rhetorikClient;
    private readonly INLSearchService _nlSearchService;
    private readonly IOutreachService _outreachService;

    public LeadsController(AutoSourcingDbContext dbContext, IRhetorikClient rhetorikClient, INLSearchService nlSearchService, IOutreachService outreachService)
    {
        _dbContext = dbContext;
        _rhetorikClient = rhetorikClient;
        _nlSearchService = nlSearchService;
        _outreachService = outreachService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedLeads>> GetLeads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] int? campaignId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        [FromQuery] DateTime? addedFrom = null,
        [FromQuery] DateTime? addedTo = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Leads.AsNoTracking();

        if (campaignId is not null)
        {
            query = query.Where(l => l.OutreachMessages.Any(m => m.CampaignId == campaignId.Value));
        }

        if (addedFrom is not null)
        {
            query = query.Where(l => l.CreatedAt >= addedFrom.Value.Date);
        }

        if (addedTo is not null)
        {
            var toExclusive = addedTo.Value.Date.AddDays(1);
            query = query.Where(l => l.CreatedAt < toExclusive);
        }

        query = ApplySort(query, sortBy, sortOrder);

        var totalCount = await query.CountAsync(cancellationToken);
        var leads = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var leadIds = leads.Select(l => l.Id).ToList();
        var campaignRows = leadIds.Count == 0
            ? new List<CampaignMembershipRow>()
            : await _dbContext.OutreachMessages
                .Where(m => leadIds.Contains(m.LeadId))
                .Select(m => new CampaignMembershipRow(m.LeadId, m.CampaignId, m.Campaign.Name))
                .Distinct()
                .ToListAsync(cancellationToken);

        var campaignsByLead = campaignRows
            .GroupBy(r => r.LeadId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new CampaignRef { Id = r.CampaignId, Name = r.CampaignName })
                    .OrderBy(c => c.Name)
                    .ToList());

        var items = leads.Select(l => new LeadDto
        {
            Id = l.Id,
            FirstName = l.FirstName,
            LastName = l.LastName,
            Email = l.Email,
            Phone = l.Phone,
            Company = l.Company,
            JobTitle = l.JobTitle,
            LinkedInUrl = l.LinkedInUrl,
            Source = l.Source,
            ExternalId = l.ExternalId,
            Status = l.Status,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,
            Campaigns = campaignsByLead.GetValueOrDefault(l.Id) ?? new List<CampaignRef>(),
        }).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new PaginatedLeads
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Lead>> GetLead(int id, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPost("search-rhetorik")]
    public async Task<ActionResult<ProfileSearchResponseEnriched>> SearchRhetorik([FromBody] ProfileSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await _rhetorikClient.SearchProfilesAsync(request, cancellationToken);

        var profileIds = response.Results
            .Select(r => r.ProfileData?.ProfileId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Cast<string>()
            .ToList();

        var leadIdByExternalId = new Dictionary<string, int>();
        var campaignMap = new Dictionary<string, List<CampaignRef>>();

        if (profileIds.Count > 0)
        {
            leadIdByExternalId = await _dbContext.Leads
                .Where(l => l.ExternalId != null && profileIds.Contains(l.ExternalId))
                .ToDictionaryAsync(l => l.ExternalId!, l => l.Id, cancellationToken);

            var campaignRows = await _dbContext.OutreachMessages
                .Where(m => m.Lead.ExternalId != null && profileIds.Contains(m.Lead.ExternalId))
                .Select(m => new { m.Lead.ExternalId, m.CampaignId, m.Campaign.Name })
                .Distinct()
                .ToListAsync(cancellationToken);

            campaignMap = campaignRows
                .GroupBy(x => x.ExternalId!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new CampaignRef { Id = x.CampaignId, Name = x.Name })
                        .OrderBy(c => c.Name)
                        .ToList());
        }

        var results = response.Results.Select(r =>
        {
            var profileId = r.ProfileData?.ProfileId;
            var leadId = profileId is not null && leadIdByExternalId.TryGetValue(profileId, out var lid) ? lid : (int?)null;
            var campaigns = profileId is not null && campaignMap.TryGetValue(profileId, out var list) ? list : new List<CampaignRef>();

            return new RhetorikProfileResultEnriched
            {
                Position = r.Position,
                ProfileData = r.ProfileData,
                ContactData = r.ContactData,
                LeadId = leadId,
                Campaigns = campaigns
            };
        }).ToList();

        return Ok(new ProfileSearchResponseEnriched
        {
            Counts = response.Counts,
            Pagination = response.Pagination,
            Results = results
        });
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

    [HttpPost("import-to-campaign")]
    public async Task<ActionResult<ImportToCampaignResponse>> ImportToCampaign([FromBody] ImportToCampaignRequest request, CancellationToken cancellationToken)
    {
        if (request.CampaignId <= 0)
        {
            return BadRequest(new { error = "A valid campaign is required." });
        }

        if (request.ProfileIds.Count == 0)
        {
            return BadRequest(new { error = "No profiles selected." });
        }

        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);
        if (campaign is null)
        {
            return BadRequest(new { error = "Campaign not found." });
        }

        var searchRequest = new ProfileSearchRequest
        {
            ProfileIds = request.ProfileIds.Distinct().ToList(),
            MaxResults = request.ProfileIds.Count > 0 ? request.ProfileIds.Count : 1
        };

        var candidates = await _rhetorikClient.SearchAndMapToLeadsAsync(searchRequest, cancellationToken);
        var candidateExternalIds = candidates
            .Select(l => l.ExternalId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Cast<string>()
            .Distinct()
            .ToList();

        var existingByExternalId = await _dbContext.Leads
            .Where(l => l.ExternalId != null && candidateExternalIds.Contains(l.ExternalId))
            .ToDictionaryAsync(l => l.ExternalId!, cancellationToken);

        var newLeads = new List<Lead>();
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate.ExternalId))
            {
                continue;
            }

            if (!existingByExternalId.ContainsKey(candidate.ExternalId))
            {
                newLeads.Add(candidate);
            }
        }

        if (newLeads.Count > 0)
        {
            _dbContext.Leads.AddRange(newLeads);
            await _dbContext.SaveChangesAsync(cancellationToken);
            foreach (var lead in newLeads)
            {
                existingByExternalId[lead.ExternalId!] = lead;
            }
        }

        var leadIds = existingByExternalId.Values.Select(l => l.Id).Distinct().ToList();
        var created = await _outreachService.AddLeadsToCampaignAsync(request.CampaignId, leadIds, cancellationToken);
        var skipped = leadIds.Count - created.Count;

        return Ok(new ImportToCampaignResponse { Added = created.Count, Skipped = skipped });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeadStatusRequest request, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FindAsync([id], cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        lead.Status = request.Status;
        lead.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static IQueryable<Lead> ApplySort(IQueryable<Lead> query, string? sortBy, string? sortOrder)
    {
        var descending = !string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        switch (sortBy?.ToLowerInvariant())
        {
            case "name":
                return descending
                    ? query.OrderByDescending(l => l.FirstName).ThenByDescending(l => l.LastName)
                    : query.OrderBy(l => l.FirstName).ThenBy(l => l.LastName);
            case "email":
                return descending ? query.OrderByDescending(l => l.Email) : query.OrderBy(l => l.Email);
            case "company":
                return descending ? query.OrderByDescending(l => l.Company) : query.OrderBy(l => l.Company);
            case "jobtitle":
                return descending ? query.OrderByDescending(l => l.JobTitle) : query.OrderBy(l => l.JobTitle);
            case "status":
                return descending ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status);
            case "dateadded":
                return descending ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt);
            case "campaigns":
                return descending
                    ? query.OrderByDescending(l => l.OutreachMessages.Count)
                    : query.OrderBy(l => l.OutreachMessages.Count);
            default:
                return descending ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt);
        }
    }
}

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
    public string? Location { get; set; }
    public string? LinkedInUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public LeadStatus Status { get; set; }
    public ConsentChannel? PreferredChannel { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<CampaignRef> Campaigns { get; set; } = [];
    public List<LeadEmailDto> Emails { get; set; } = [];
}

public class LeadEmailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsPrimary { get; set; }
    public int Priority { get; set; }
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

    [JsonPropertyName("resume_data")]
    public RhetorikResumeData? ResumeData { get; set; }

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

public class UpdateLeadRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
}

public class RefreshLeadsRequest
{
    public List<int> LeadIds { get; set; } = [];
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

        var emailRows = leadIds.Count == 0
            ? new List<LeadEmail>()
            : await _dbContext.LeadEmails
                .Where(e => leadIds.Contains(e.LeadId))
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.Id)
                .ToListAsync(cancellationToken);

        var emailsByLead = emailRows
            .GroupBy(e => e.LeadId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => new LeadEmailDto
                {
                    Id = e.Id,
                    Email = e.Email,
                    Type = e.Type,
                    IsVerified = e.IsVerified,
                    IsPrimary = e.IsPrimary,
                    Priority = e.Priority
                }).ToList());

        var items = leads.Select(l => new LeadDto
        {
            Id = l.Id,
            FirstName = l.FirstName,
            LastName = l.LastName,
            Email = l.Email,
            Phone = l.Phone,
            Company = l.Company,
            JobTitle = l.JobTitle,
            Location = l.Location,
            LinkedInUrl = l.LinkedInUrl,
            Source = l.Source,
            ExternalId = l.ExternalId,
            Status = l.Status,
            PreferredChannel = l.PreferredChannel,
            Country = l.Country,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,
            Campaigns = campaignsByLead.GetValueOrDefault(l.Id) ?? new List<CampaignRef>(),
            Emails = emailsByLead.GetValueOrDefault(l.Id) ?? new List<LeadEmailDto>(),
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

    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult<LeadProfile>> GetLeadProfile(int id, CancellationToken cancellationToken)
    {
        var profile = await _dbContext.LeadProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.LeadId == id, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
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
                ResumeData = r.ResumeData,
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
            if (!string.IsNullOrWhiteSpace(candidate.LinkedInUrl) && string.IsNullOrWhiteSpace(existing.LinkedInUrl))
            {
                existing.LinkedInUrl = candidate.LinkedInUrl;
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

    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshLeads([FromBody] RefreshLeadsRequest request, CancellationToken cancellationToken)
    {
        if (request.LeadIds.Count == 0)
        {
            return BadRequest(new { error = "No leads selected." });
        }

        var leads = await _dbContext.Leads
            .Include(l => l.Profile)
            .Where(l => request.LeadIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        if (leads.Count == 0)
        {
            return NotFound();
        }

        var externalIds = leads
            .Where(l => !string.IsNullOrEmpty(l.ExternalId))
            .Select(l => l.ExternalId!)
            .ToList();

        if (externalIds.Count == 0)
        {
            return Ok(new { updated = 0, message = "No leads have Rhetorik profile IDs." });
        }

        var searchRequest = new ProfileSearchRequest { ProfileIds = externalIds };
        var results = await _rhetorikClient.SearchProfilesAsync(searchRequest, cancellationToken);

        var updated = 0;
        foreach (var result in results.Results)
        {
            var profileId = result.ProfileData?.ProfileId;
            if (string.IsNullOrEmpty(profileId)) continue;

            var lead = leads.FirstOrDefault(l => l.ExternalId == profileId);
            if (lead is null) continue;

            var p = result.ProfileData!;
            var currentExp = result.ContactData?.CurrentExperiences?
                .FirstOrDefault(e => e.Current == true) ?? result.ContactData?.CurrentExperiences?.FirstOrDefault();

            // Only update profile data from Rhetorik — do NOT overwrite manually added contact details
            lead.Company = TruncateNullable(currentExp?.RawCompanyName ?? currentExp?.CompanyName, 200) ?? lead.Company;
            lead.JobTitle = TruncateNullable(currentExp?.JobTitle ?? p.Headline, 200) ?? lead.JobTitle;
            lead.Location = TruncateNullable(BuildLocation(p.Address), 200) ?? lead.Location;
            lead.LinkedInUrl = RhetorikSocialLinks.ExtractLinkedInUrl(p.SocialLinks) ?? lead.LinkedInUrl;
            lead.UpdatedAt = DateTime.UtcNow;

            var workExp = result.ResumeData?.Experiences?
                .Select(e => new { company = e.RawCompanyName ?? e.CompanyName, title = e.JobTitle, current = e.Current ?? false, startDate = e.StartDate, endDate = e.EndDate })
                .ToList();

            var education = result.ResumeData?.Educations?
                .Select(e => new { school = e.EducationalEstablishment, degree = e.Diploma, specialization = e.Specialization, startDate = e.StartDate, endDate = e.EndDate })
                .ToList();

            if (lead.Profile is null)
            {
                lead.Profile = new LeadProfile { LeadId = lead.Id };
                _dbContext.LeadProfiles.Add(lead.Profile);
            }

            lead.Profile.Headline = TruncateNullable(p.Headline, 500) ?? lead.Profile.Headline;
            lead.Profile.Summary = p.Summary ?? lead.Profile.Summary;
            lead.Profile.SelfReportedSkills = p.Expertises is { Count: > 0 } ? string.Join(", ", p.Expertises) : lead.Profile.SelfReportedSkills;
            lead.Profile.WorkExperience = workExp is { Count: > 0 } ? System.Text.Json.JsonSerializer.Serialize(workExp) : lead.Profile.WorkExperience;
            lead.Profile.Education = education is { Count: > 0 } ? System.Text.Json.JsonSerializer.Serialize(education) : lead.Profile.Education;
            lead.Profile.Certifications = result.ResumeData?.Certifications?.Select(c => c.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() is { Count: > 0 } certs
                ? string.Join(", ", certs) : lead.Profile.Certifications;
            lead.Profile.Languages = p.Languages is { Count: > 0 } langs
                ? string.Join(", ", langs) : lead.Profile.Languages;

            updated++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { updated });
    }
    // Removes contact emails discovered from Rhetorik. Outreach always uses the lead's own email
    // now, so these are no longer used for sending and are safe to clear.
    // Notes and conversation history stored against the candidate (includes AI interview summaries).
    [HttpGet("{id:int}/notes")]
    public async Task<ActionResult> Notes(int id, CancellationToken cancellationToken)
    {
        var notes = await _dbContext.ConversationMessages
            .AsNoTracking()
            .Where(c => c.LeadId == id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Role, c.Content, c.IsEscalation, c.CreatedAt })
            .ToListAsync(cancellationToken);

        return Ok(notes);
    }

    [HttpDelete("discovered-emails")]
    public async Task<ActionResult> ClearDiscoveredEmails(CancellationToken cancellationToken)
    {
        var emails = await _dbContext.LeadEmails.ToListAsync(cancellationToken);
        if (emails.Count > 0)
        {
            _dbContext.LeadEmails.RemoveRange(emails);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { removed = emails.Count });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeadStatusRequest request, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        lead.Status = request.Status;
        lead.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LeadDto>> UpdateLead(int id, [FromBody] UpdateLeadRequest request, CancellationToken cancellationToken)
    {
        var lead = await _dbContext.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName)) lead.FirstName = request.FirstName;
        if (!string.IsNullOrWhiteSpace(request.LastName)) lead.LastName = request.LastName;
        if (!string.IsNullOrWhiteSpace(request.Email)) lead.Email = request.Email;
        lead.Phone = request.Phone;
        lead.LinkedInUrl = request.LinkedInUrl;
        lead.Company = request.Company;
        lead.JobTitle = request.JobTitle;
        lead.Location = request.Location;
        lead.Country = request.Country;
        lead.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new LeadDto
        {
            Id = lead.Id,
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            Phone = lead.Phone,
            Company = lead.Company,
            JobTitle = lead.JobTitle,
            Location = lead.Location,
            LinkedInUrl = lead.LinkedInUrl,
            Country = lead.Country,
            Source = lead.Source,
            ExternalId = lead.ExternalId,
            Status = lead.Status,
            PreferredChannel = lead.PreferredChannel,
            CreatedAt = lead.CreatedAt,
            UpdatedAt = lead.UpdatedAt
        });
    }

    private static string? TruncateNullable(string? value, int maxLength)
    {
        return value is null ? null : value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? BuildLocation(RhetorikAddress? address)
    {
        if (address is null) return null;
        var parts = new[] { address.City, address.State, address.Country }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToList();
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
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

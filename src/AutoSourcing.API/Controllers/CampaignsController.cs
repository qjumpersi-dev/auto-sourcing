using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;

    public CampaignsController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Campaign>>> GetCampaigns(CancellationToken cancellationToken)
    {
        return Ok(await _dbContext.Campaigns.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Campaign>> GetCampaign(int id, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns
            .Include(c => c.OutreachMessages)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return campaign is null ? NotFound() : Ok(campaign);
    }

    [HttpPost]
    public async Task<ActionResult<Campaign>> CreateCampaign([FromBody] Campaign campaign, CancellationToken cancellationToken)
    {
        campaign.Id = 0;
        campaign.CreatedAt = DateTime.UtcNow;
        _dbContext.Campaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCampaign(int id, [FromBody] Campaign updated, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns.FindAsync([id], cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        campaign.Name = updated.Name;
        campaign.Description = updated.Description;
        campaign.Status = updated.Status;
        if (campaign.Status == Core.Enums.CampaignStatus.Active && campaign.StartedAt is null)
        {
            campaign.StartedAt = DateTime.UtcNow;
        }
        if (campaign.Status == Core.Enums.CampaignStatus.Completed && campaign.CompletedAt is null)
        {
            campaign.CompletedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

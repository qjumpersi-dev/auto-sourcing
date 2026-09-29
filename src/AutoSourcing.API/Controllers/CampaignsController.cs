using AutoSourcing.API.BackgroundServices;
using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Outreach;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly ICampaignRunQueue _campaignRunQueue;
    private readonly IOutreachService _outreachService;

    public CampaignsController(AutoSourcingDbContext dbContext, ICampaignRunQueue campaignRunQueue, IOutreachService outreachService)
    {
        _dbContext = dbContext;
        _campaignRunQueue = campaignRunQueue;
        _outreachService = outreachService;
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
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return campaign is null ? NotFound() : Ok(campaign);
    }

    [HttpPost]
    public async Task<ActionResult<Campaign>> CreateCampaign([FromBody] Campaign campaign, CancellationToken cancellationToken)
    {
        if (campaign.SequenceId is int sequenceId && !await _dbContext.Sequences.AnyAsync(s => s.Id == sequenceId, cancellationToken))
        {
            return BadRequest(new { error = "Sequence not found." });
        }

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

        if (updated.SequenceId is int sequenceId && !await _dbContext.Sequences.AnyAsync(s => s.Id == sequenceId, cancellationToken))
        {
            return BadRequest(new { error = "Sequence not found." });
        }

        campaign.Name = updated.Name;
        campaign.Description = updated.Description;
        campaign.Status = updated.Status;
        campaign.Channel = updated.Channel;
        var sequenceChanged = campaign.SequenceId != updated.SequenceId;
        campaign.SequenceId = updated.SequenceId;
        if (campaign.Status == CampaignStatus.Active && campaign.StartedAt is null)
        {
            campaign.StartedAt = DateTime.UtcNow;
        }
        if (campaign.Status == CampaignStatus.Completed && campaign.CompletedAt is null)
        {
            campaign.CompletedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (sequenceChanged)
        {
            await _outreachService.RegenerateDraftsAsync(id, cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("{id:int}/restart")]
    public async Task<IActionResult> RestartCampaign(int id, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.SequenceId is null)
        {
            return BadRequest(new { error = "Add a sequence before restarting this campaign." });
        }

        if (campaign.IsRunning)
        {
            return BadRequest(new { error = "This campaign is already running." });
        }

        var count = await _outreachService.RestartAsync(id, cancellationToken);
        if (count == 0)
        {
            return BadRequest(new { error = "No candidates in this campaign yet." });
        }

        campaign.IsRunning = true;
        campaign.LastRunAt = DateTime.UtcNow;
        campaign.Status = CampaignStatus.Active;
        campaign.StartedAt ??= DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _campaignRunQueue.EnqueueAsync(id, cancellationToken);

        return Accepted(new { restarted = true, candidates = count });
    }

    [HttpPost("{id:int}/run")]
    public async Task<IActionResult> RunCampaign(int id, CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.SequenceId is null)
        {
            return BadRequest(new { error = "Add a sequence before running this campaign." });
        }

        if (campaign.IsRunning)
        {
            return Ok(new { queued = false, alreadyRunning = true });
        }

        var hasSteps = await _dbContext.SequenceSteps
            .AnyAsync(s => s.SequenceId == campaign.SequenceId.Value, cancellationToken);
        if (!hasSteps)
        {
            return BadRequest(new { error = "The campaign's sequence has no steps yet." });
        }

        campaign.IsRunning = true;
        campaign.LastRunAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _campaignRunQueue.EnqueueAsync(id, cancellationToken);

        return Accepted(new { queued = true });
    }
}

public class AddLeadsRequest
{
    public int[] LeadIds { get; set; } = [];
}

[ApiController]
[Route("api/campaigns/{campaignId:int}/leads")]
public class CampaignLeadsController : ControllerBase
{
    private readonly IOutreachService _outreachService;

    public CampaignLeadsController(IOutreachService outreachService)
    {
        _outreachService = outreachService;
    }

    [HttpPost]
    public async Task<IActionResult> AddLeads(int campaignId, [FromBody] AddLeadsRequest request, CancellationToken cancellationToken)
    {
        if (request.LeadIds.Length == 0)
        {
            return BadRequest(new { error = "No leads selected." });
        }

        try
        {
            var created = await _outreachService.AddLeadsToCampaignAsync(campaignId, request.LeadIds, cancellationToken);
            return Ok(new { added = created.Count, skipped = request.LeadIds.Length - created.Count });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

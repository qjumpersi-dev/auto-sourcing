using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using AutoSourcing.Services.Jobs;
using AutoSourcing.Services.Rhetorik;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IJobService _jobService;

    public JobsController(AutoSourcingDbContext dbContext, IJobService jobService)
    {
        _dbContext = dbContext;
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobs(CancellationToken cancellationToken)
    {
        return Ok(await _dbContext.Jobs.AsNoTracking().OrderByDescending(j => j.CreatedAt).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Job>> GetJob(int id, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<Job>> CreateJob([FromBody] Job job, CancellationToken cancellationToken)
    {
        job.Id = 0;
        job.CreatedAt = DateTime.UtcNow;
        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateJob(int id, [FromBody] Job updated, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs.FindAsync([id], cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        job.Title = updated.Title;
        job.Location = updated.Location;
        job.Industry = updated.Industry;
        job.Type = updated.Type;
        job.Flexibility = updated.Flexibility;
        job.SalaryType = updated.SalaryType;
        job.SalaryFrom = updated.SalaryFrom;
        job.SalaryTo = updated.SalaryTo;
        job.SalaryNotes = updated.SalaryNotes;
        job.StartDate = updated.StartDate;
        job.ExpiryDate = updated.ExpiryDate;
        job.HiringManager = updated.HiringManager;
        job.Department = updated.Department;
        job.AdvertUrl = updated.AdvertUrl;
        job.AdvertCopy = updated.AdvertCopy;
        job.MustHaves = updated.MustHaves;
        job.NiceToHaves = updated.NiceToHaves;
        job.Education = updated.Education;
        job.Skills = updated.Skills;
        job.AttractiveReasons = updated.AttractiveReasons;
        job.ScreeningDetails = updated.ScreeningDetails;
        job.Status = updated.Status;
        job.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteJob(int id, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs.FindAsync([id], cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        _dbContext.Jobs.Remove(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/generate-search")]
    public async Task<ActionResult<ProfileSearchRequest>> GenerateSearch(int id, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs.FindAsync([id], cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var request = _jobService.BuildSearchSpecFromJob(job);
        return Ok(request);
    }
}

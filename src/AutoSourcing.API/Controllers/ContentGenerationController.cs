using AutoSourcing.Data;
using AutoSourcing.Services.ContentGeneration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/sequences")]
public class ContentGenerationController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IContentGenerationService _contentGenerationService;

    public ContentGenerationController(AutoSourcingDbContext dbContext, IContentGenerationService contentGenerationService)
    {
        _dbContext = dbContext;
        _contentGenerationService = contentGenerationService;
    }

    [HttpPost("generate-content")]
    public async Task<ActionResult<GeneratedContent>> GenerateContent([FromBody] GenerateContentRequest request, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs.FindAsync([request.JobId], cancellationToken);
        if (job is null)
        {
            return BadRequest(new { error = $"Job {request.JobId} not found." });
        }

        var organization = await _dbContext.OrganizationProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        try
        {
            var content = await _contentGenerationService.GenerateAsync(request, job, organization, cancellationToken);
            return Ok(content);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Content generation failed: {ex.Message}" });
        }
    }
}

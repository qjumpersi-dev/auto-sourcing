using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;

    public OrganizationController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<OrganizationProfile>> Get(CancellationToken cancellationToken)
    {
        var profile = await _dbContext.OrganizationProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            profile = new OrganizationProfile();
            _dbContext.OrganizationProfiles.Add(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<OrganizationProfile>> Update([FromBody] OrganizationProfile updated, CancellationToken cancellationToken)
    {
        var profile = await _dbContext.OrganizationProfiles.FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            profile = new OrganizationProfile();
            _dbContext.OrganizationProfiles.Add(profile);
        }

        profile.OrgName = updated.OrgName;
        profile.About = updated.About;
        profile.EVP = updated.EVP;
        profile.Culture = updated.Culture;
        profile.HiringProcess = updated.HiringProcess;
        profile.EEO = updated.EEO;
        profile.GuardRails = updated.GuardRails;
        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(profile);
    }
}

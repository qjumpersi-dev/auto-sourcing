using AutoSourcing.Services.LinkedIn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/linkedin")]
public class LinkedInController : ControllerBase
{
    private readonly ILinkedInService _linkedInService;
    private readonly LinkedInOptions _options;

    public LinkedInController(ILinkedInService linkedInService, IOptions<LinkedInOptions> options)
    {
        _linkedInService = linkedInService;
        _options = options.Value;
    }

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var signedIn = await _linkedInService.IsSignedInAsync(cancellationToken);
        return Ok(new
        {
            signedIn,
            dryRun = _options.DryRun,
            userDataDir = _options.UserDataDir
        });
    }

    [HttpPost("sign-in")]
    public async Task<ActionResult> SignIn(CancellationToken cancellationToken)
    {
        var signedIn = await _linkedInService.SignInAsync(cancellationToken);
        return Ok(new { signedIn });
    }

    [HttpGet("debug")]
    public async Task<ActionResult> Debug(CancellationToken cancellationToken)
    {
        var pages = await _linkedInService.GetOpenPagesAsync(cancellationToken);
        return Ok(pages);
    }

    [HttpGet("dom")]
    public async Task<ActionResult> Dom([FromQuery] string? url, CancellationToken cancellationToken)
    {
        var probe = await _linkedInService.ProbeDomAsync(url ?? string.Empty, cancellationToken);
        return Ok(probe);
    }
}
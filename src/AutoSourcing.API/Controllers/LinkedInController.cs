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
    private readonly ILogger<LinkedInController> _logger;

    public LinkedInController(ILinkedInService linkedInService, IOptions<LinkedInOptions> options, ILogger<LinkedInController> logger)
    {
        _linkedInService = linkedInService;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus(CancellationToken cancellationToken)
    {
        try
        {
            var signedIn = await _linkedInService.IsSignedInAsync(cancellationToken);
            return Ok(new
            {
                signedIn,
                dryRun = _options.DryRun,
                userDataDir = _options.UserDataDir,
                available = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LinkedIn automation is not available in this environment.");
            return Ok(new
            {
                signedIn = false,
                dryRun = _options.DryRun,
                userDataDir = _options.UserDataDir,
                available = false,
                error = "LinkedIn automation is not available in this environment."
            });
        }
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
using AutoSourcing.Services.Scotty;
using Microsoft.AspNetCore.Mvc;

namespace AutoSourcing.API.Controllers;

public class ScottyChatRequestDto
{
    public string UserPrompt { get; set; } = string.Empty;
    public string? ContinuityKey { get; set; }
}

public class ScottyCallRequestDto
{
    public string? ContinuityKey { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ScottyController : ControllerBase
{
    private readonly IScottyClient _scottyClient;

    public ScottyController(IScottyClient scottyClient)
    {
        _scottyClient = scottyClient;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ScottyChatRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            return BadRequest(new { error = "UserPrompt is required." });
        }

        var continuityKey = request.ContinuityKey ?? Guid.NewGuid().ToString();
        if (!Guid.TryParse(continuityKey, out _))
        {
            return BadRequest(new { error = "ContinuityKey must be a valid UUID." });
        }

        var result = await _scottyClient.SendTextAsync(new ScottyChatRequest
        {
            UserPrompt = request.UserPrompt,
            ContinuityKey = continuityKey
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("call")]
    public async Task<IActionResult> Call([FromBody] ScottyCallRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _scottyClient.GetCallCredentialAsync(new ScottyCallRequest
        {
            SessionParticipantId = Guid.NewGuid().ToString(),
            ContinuityKey = request.ContinuityKey
        }, cancellationToken);

        return Ok(result);
    }
}
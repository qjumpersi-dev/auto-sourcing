using AutoSourcing.Services.Rhetorik;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AutoSourcing.API.Controllers;

public class AutocompleteQuery
{
    public string Field { get; set; } = "countries";
    public string InputText { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class RhetorikController : ControllerBase
{
    private static readonly string[] AllowedFields =
    [
        "countries", "states", "cities", "skill_names", "industries_names"
    ];

    private readonly IRhetorikClient _rhetorikClient;
    private readonly IMemoryCache _cache;

    public RhetorikController(IRhetorikClient rhetorikClient, IMemoryCache cache)
    {
        _rhetorikClient = rhetorikClient;
        _cache = cache;
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string field = "countries", [FromQuery] string inputText = "", CancellationToken cancellationToken = default)
    {
        if (!AllowedFields.Contains(field, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = $"Field must be one of: {string.Join(", ", AllowedFields)}" });
        }

        var trimmed = inputText.Trim();
        if (trimmed.Length == 0)
        {
            return Ok(Array.Empty<AutocompleteSuggestion>());
        }

        var cacheKey = $"autocomplete:{field.ToLowerInvariant()}:{trimmed.ToLowerInvariant()}";
        var suggestions = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return await _rhetorikClient.AutocompleteAsync(field.ToLowerInvariant(), trimmed, cancellationToken);
        });

        return Ok(suggestions);
    }
}

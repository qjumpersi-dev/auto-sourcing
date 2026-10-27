using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class AddLookupRequest
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class LookupsController : ControllerBase
{
    // Seeded from the Interview-Ready Data Centre Talent Strategy. Users can add more.
    private static readonly Dictionary<string, string[]> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["job-title"] =
        [
            "Critical Facilities Engineer",
            "Commissioning Engineer",
            "Controls and BMS Engineer",
            "Field Service Engineer - Power and Cooling",
            "Data Centre Technician",
            "HVAC Technician",
            "Electrical Technician"
        ],
        ["role-family"] =
        [
            "Critical Facilities Engineer",
            "Commissioning Engineer",
            "Controls and BMS Engineer",
            "Field Service Engineer",
            "Data Centre Technician",
            "Technician",
            "Engineer",
            "Other"
        ],
        ["industry"] =
        [
            "Data Centres",
            "Colocation",
            "Facilities Management",
            "Equipment Manufacturing & Service",
            "Hospitals & Healthcare",
            "Telecommunications",
            "Semiconductors",
            "Utilities & Power Generation",
            "Pharmaceutical Manufacturing",
            "Airports",
            "Renewable Energy",
            "Large Commercial Buildings",
            "Industrial Automation",
            "Industrial Maintenance",
            "Defence Communications",
            "Network Operations Centre",
            "Hosting & Managed Services",
            "Technical Field Services"
        ],
    };

    private readonly AutoSourcingDbContext _dbContext;

    public LookupsController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LookupValue>>> GetLookups(
        [FromQuery] string category,
        CancellationToken cancellationToken)
    {
        var key = (category ?? string.Empty).Trim();
        if (key.Length == 0)
        {
            return Ok(Array.Empty<LookupValue>());
        }

        var existing = await _dbContext.LookupValues
            .Where(v => v.Category == key)
            .OrderBy(v => v.Value)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0 && Defaults.TryGetValue(key, out var defaults))
        {
            var seeded = defaults
                .Select(value => new LookupValue { Category = key, Value = value })
                .ToList();

            _dbContext.LookupValues.AddRange(seeded);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(seeded.OrderBy(v => v.Value).ToList());
        }

        return Ok(existing);
    }

    [HttpPost]
    public async Task<ActionResult<LookupValue>> Add([FromBody] AddLookupRequest request, CancellationToken cancellationToken)
    {
        var category = (request.Category ?? string.Empty).Trim();
        var value = (request.Value ?? string.Empty).Trim();

        if (category.Length == 0 || value.Length == 0)
        {
            return BadRequest(new { error = "Category and value are required." });
        }

        var existing = await _dbContext.LookupValues
            .FirstOrDefaultAsync(v => v.Category == category && v.Value.ToLower() == value.ToLower(), cancellationToken);

        if (existing is not null)
        {
            return Ok(existing);
        }

        var created = new LookupValue { Category = category, Value = value };
        _dbContext.LookupValues.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(created);
    }
}
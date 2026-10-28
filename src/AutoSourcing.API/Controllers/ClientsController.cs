using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;

    public ClientsController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Client>>> GetClients(CancellationToken cancellationToken)
    {
        return Ok(await _dbContext.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Client>> GetClient(int id, CancellationToken cancellationToken)
    {
        var client = await _dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> Create([FromBody] Client client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(client.Name))
        {
            return BadRequest(new { error = "Client name is required." });
        }

        client.Id = 0;
        client.Name = client.Name.Trim();
        client.CreatedAt = DateTime.UtcNow;
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Client updated, CancellationToken cancellationToken)
    {
        var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(updated.Name))
        {
            return BadRequest(new { error = "Client name is required." });
        }

        client.Name = updated.Name.Trim();
        client.ContactName = updated.ContactName;
        client.ContactEmail = updated.ContactEmail;
        client.ContactPhone = updated.ContactPhone;
        client.Notes = updated.Notes;
        client.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client is null)
        {
            return NotFound();
        }

        // Jobs that referenced the client become unassigned rather than dangling.
        var jobs = await _dbContext.Jobs.Where(j => j.ClientId == id).ToListAsync(cancellationToken);
        foreach (var job in jobs)
        {
            job.ClientId = null;
            job.UpdatedAt = DateTime.UtcNow;
        }

        _dbContext.Clients.Remove(client);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
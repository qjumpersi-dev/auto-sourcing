using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PolicyController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;

    public PolicyController(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PolicyGuardrails>> Get(CancellationToken cancellationToken)
    {
        var policy = await _dbContext.PolicyGuardrails.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            policy = new PolicyGuardrails();
            _dbContext.PolicyGuardrails.Add(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(policy);
    }

    [HttpPut]
    public async Task<ActionResult<PolicyGuardrails>> Update([FromBody] PolicyGuardrails updated, CancellationToken cancellationToken)
    {
        var policy = await _dbContext.PolicyGuardrails.FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            policy = new PolicyGuardrails();
            _dbContext.PolicyGuardrails.Add(policy);
        }

        policy.WhatAiMayAnswer = updated.WhatAiMayAnswer;
        policy.EscalationTriggers = updated.EscalationTriggers;
        policy.RefusalTopics = updated.RefusalTopics;
        policy.RequiredDisclaimers = updated.RequiredDisclaimers;
        policy.MarketRules = updated.MarketRules;
        policy.NeedsHumanStates = updated.NeedsHumanStates;
        policy.ConfidenceThreshold = updated.ConfidenceThreshold;
        policy.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(policy);
    }
}

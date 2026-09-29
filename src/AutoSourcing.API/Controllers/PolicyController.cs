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
            policy = CreateDefault();
            _dbContext.PolicyGuardrails.Add(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(policy);
    }

    private static PolicyGuardrails CreateDefault() => new()
    {
        WhatAiMayAnswer = "<p>Job details (title, location, requirements, salary if provided), company information (about, culture, benefits), and the application and interview process.</p>",
        EscalationTriggers = "<p>Compensation negotiation, visa or legal queries, complex policy questions, and complaints.</p>",
        RefusalTopics = "<p>Discriminatory topics, and personal opinions on politics, religion, or other non-work subjects.</p>",
        RequiredDisclaimers = "<p>Consent is not required to apply for a role. For SMS: message and data rates may apply, reply STOP to opt out.</p>",
        MarketRules = "<p>US: A2P-compliant opt-in required for SMS. NZ: explicit opt-in required for SMS.</p>",
        NeedsHumanStates = "<p>The candidate asks for a human, or the model is uncertain about an answer.</p>",
        ConfidenceThreshold = 80
    };

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

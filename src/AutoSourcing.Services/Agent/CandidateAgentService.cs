using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using AutoSourcing.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Services.Agent;

public record CandidateContext(
    string CandidateName,
    string? Email,
    string? Phone,
    string? Company,
    string? JobTitle,
    string? Location,
    string? Country,
    string OrgName,
    string? About,
    string? Evp,
    string? Culture,
    string? HiringProcess,
    string? WhatAiMayAnswer,
    string? EscalationTriggers,
    string? RefusalTopics,
    string? RequiredDisclaimers,
    string? MarketRules,
    string? NeedsHumanStates
);

public interface ICandidateAgentService
{
    Task<Lead?> ResolveLeadAsync(Guid conversationKey, CancellationToken cancellationToken = default);
    Task<CandidateContext?> GetContextAsync(Guid conversationKey, CancellationToken cancellationToken = default);
    Task<string> GetCompanyInfoAsync(CancellationToken cancellationToken = default);
    Task<string> GetGuardrailsAsync(CancellationToken cancellationToken = default);
    Task SaveMessageAsync(int leadId, string role, string content, bool isEscalation = false, CancellationToken cancellationToken = default);
    Task<bool> EscalateAsync(Guid conversationKey, string reason, CancellationToken cancellationToken = default);
}

public class CandidateAgentService : ICandidateAgentService
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IEmailService _emailService;

    public CandidateAgentService(AutoSourcingDbContext dbContext, IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task<Lead?> ResolveLeadAsync(Guid conversationKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ConversationKey == conversationKey, cancellationToken);
    }

    public async Task<CandidateContext?> GetContextAsync(Guid conversationKey, CancellationToken cancellationToken = default)
    {
        var lead = await _dbContext.Leads
            .Include(l => l.Profile)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ConversationKey == conversationKey, cancellationToken);

        if (lead is null)
        {
            return null;
        }

        var org = await _dbContext.OrganizationProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var policy = await _dbContext.PolicyGuardrails.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        return new CandidateContext(
            $"{lead.FirstName} {lead.LastName}".Trim(),
            lead.Email,
            lead.Phone,
            lead.Company,
            lead.JobTitle,
            lead.Location,
            lead.Country,
            org?.OrgName ?? "the company",
            StripHtml(org?.About),
            StripHtml(org?.EVP),
            StripHtml(org?.Culture),
            StripHtml(org?.HiringProcess),
            StripHtml(policy?.WhatAiMayAnswer),
            StripHtml(policy?.EscalationTriggers),
            StripHtml(policy?.RefusalTopics),
            StripHtml(policy?.RequiredDisclaimers),
            StripHtml(policy?.MarketRules),
            StripHtml(policy?.NeedsHumanStates)
        );
    }

    public async Task<string> GetCompanyInfoAsync(CancellationToken cancellationToken = default)
    {
        var org = await _dbContext.OrganizationProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return $"""
            Company: {org?.OrgName ?? "—"}
            About: {StripHtml(org?.About) ?? "—"}
            EVP / why join: {StripHtml(org?.EVP) ?? "—"}
            Culture and benefits: {StripHtml(org?.Culture) ?? "—"}
            Hiring process: {StripHtml(org?.HiringProcess) ?? "—"}
            """;
    }

    public async Task<string> GetGuardrailsAsync(CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.PolicyGuardrails.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return $"""
            What you may answer: {StripHtml(policy?.WhatAiMayAnswer) ?? "—"}
            Escalate to a recruiter when: {StripHtml(policy?.EscalationTriggers) ?? "—"}
            Topics to avoid: {StripHtml(policy?.RefusalTopics) ?? "—"}
            Required disclaimers: {StripHtml(policy?.RequiredDisclaimers) ?? "—"}
            Market / geography rules: {StripHtml(policy?.MarketRules) ?? "—"}
            Needs human when: {StripHtml(policy?.NeedsHumanStates) ?? "—"}
            """;
    }

    public async Task SaveMessageAsync(int leadId, string role, string content, bool isEscalation = false, CancellationToken cancellationToken = default)
    {
        _dbContext.ConversationMessages.Add(new ConversationMessage
        {
            LeadId = leadId,
            Role = role,
            Content = content,
            IsEscalation = isEscalation,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EscalateAsync(Guid conversationKey, string reason, CancellationToken cancellationToken = default)
    {
        var lead = await _dbContext.Leads
            .FirstOrDefaultAsync(l => l.ConversationKey == conversationKey, cancellationToken);

        if (lead is null)
        {
            return false;
        }

        _dbContext.ConversationMessages.Add(new ConversationMessage
        {
            LeadId = lead.Id,
            Role = "system",
            Content = $"Escalated to recruiter: {reason}",
            IsEscalation = true,
            CreatedAt = DateTime.UtcNow
        });

        lead.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var org = await _dbContext.OrganizationProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var to = org?.EscalationEmail;
        if (!string.IsNullOrWhiteSpace(to))
        {
            try
            {
                var subject = $"Candidate escalation: {lead.FirstName} {lead.LastName}";
                var body = $"""
                    <p>A candidate conversation has been escalated to a recruiter.</p>
                    <p><strong>Candidate:</strong> {lead.FirstName} {lead.LastName} ({lead.Email})</p>
                    <p><strong>Reason:</strong> {reason}</p>
                    <p><strong>Current role:</strong> {lead.JobTitle ?? "—"} at {lead.Company ?? "—"}</p>
                    """;
                await _emailService.SendAsync(new[] { to }, subject, body, cancellationToken: cancellationToken);
            }
            catch
            {
                // Escalation is recorded even if the email fails.
            }
        }

        return true;
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
    }
}

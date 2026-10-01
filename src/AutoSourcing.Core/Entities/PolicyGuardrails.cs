using AutoSourcing.Core.Abstractions;

namespace AutoSourcing.Core.Entities;

public class PolicyGuardrails : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? WhatAiMayAnswer { get; set; }
    public string? EscalationTriggers { get; set; }
    public string? RefusalTopics { get; set; }
    public string? RequiredDisclaimers { get; set; }
    public string? MarketRules { get; set; }
    public string? NeedsHumanStates { get; set; }
    public decimal? ConfidenceThreshold { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

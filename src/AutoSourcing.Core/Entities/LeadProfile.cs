using AutoSourcing.Core.Abstractions;

namespace AutoSourcing.Core.Entities;

public class LeadProfile : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public string? Headline { get; set; }
    public string? Summary { get; set; }
    public string? SelfReportedSkills { get; set; }
    public string? AIInferredSkills { get; set; }
    public string? WorkExperience { get; set; }
    public string? Education { get; set; }
    public string? Certifications { get; set; }
    public string? Industries { get; set; }
    public string? Languages { get; set; }
    public string? Memberships { get; set; }
    public string? Publications { get; set; }
    public string? Awards { get; set; }
    public string? Patents { get; set; }
    public DateTime? LastUpdatedByCandidate { get; set; }
}

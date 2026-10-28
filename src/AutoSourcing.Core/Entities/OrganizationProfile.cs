using AutoSourcing.Core.Abstractions;

namespace AutoSourcing.Core.Entities;

public class OrganizationProfile : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? OrgName { get; set; }
    public string? EscalationEmail { get; set; }
    public string? About { get; set; }
    public string? EVP { get; set; }
    public string? Culture { get; set; }
    public string? HiringProcess { get; set; }
    public string? EEO { get; set; }
    public string? GuardRails { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

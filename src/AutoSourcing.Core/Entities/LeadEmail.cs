using AutoSourcing.Core.Abstractions;

namespace AutoSourcing.Core.Entities;

public class LeadEmail : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsPrimary { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
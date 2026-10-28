using AutoSourcing.Core.Abstractions;

namespace AutoSourcing.Core.Entities;

// A reusable drop-down value (job title, industry, role family) so near-duplicate spellings can't
// fragment reporting. Values are per user and can be added from the UI.
public class LookupValue : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
using AutoSourcing.Core.Abstractions;

namespace AutoSourcing.Core.Entities;

public class ConversationMessage : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public bool IsEscalation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

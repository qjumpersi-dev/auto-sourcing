using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class ChannelConsent : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public ConsentChannel Channel { get; set; }
    public ConsentStatus Status { get; set; } = ConsentStatus.Unknown;
    public string? OptInSource { get; set; }
    public DateTime? OptInDate { get; set; }
    public DateTime? OptOutDate { get; set; }
    public string? Notes { get; set; }
}

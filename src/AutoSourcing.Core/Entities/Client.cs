using AutoSourcing.Core.Abstractions;

namespace AutoSourcing.Core.Entities;

// The hiring organisation a job belongs to, so jobs can be grouped and reported by client.
public class Client : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Notes { get; set; }

    // Company story the candidate agent uses when answering about the hiring client.
    public string? About { get; set; }
    public string? Evp { get; set; }
    public string? Culture { get; set; }
    public string? HiringProcess { get; set; }
    public string? Eeo { get; set; }
    public string? GuardRails { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
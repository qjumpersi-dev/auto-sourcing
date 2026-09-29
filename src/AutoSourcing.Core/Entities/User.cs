using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Recruiter;
    public bool IsActive { get; set; } = true;

    // Sending identity ("send as") for outreach emails.
    public string? SendFromAddress { get; set; }
    public string? SendFromName { get; set; }
    public string? ReplyToAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}

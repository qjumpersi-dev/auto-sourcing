namespace AutoSourcing.Services.Email;

// The identity an outreach email is sent as. Nulls fall back to the platform defaults.
public record SenderIdentity(int? UserId, string? FromAddress, string? FromName, string? ReplyTo)
{
    public static readonly SenderIdentity Platform = new(null, null, null, null);
}

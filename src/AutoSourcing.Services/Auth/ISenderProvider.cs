using AutoSourcing.Services.Email;

namespace AutoSourcing.Services.Auth;

// Resolves the sending identity for the current request. Falls back to the platform default
// when there is no authenticated user (background jobs, API-key callers).
public interface ISenderProvider
{
    SenderIdentity Current { get; }
}

public class NullSenderProvider : ISenderProvider
{
    public SenderIdentity Current => SenderIdentity.Platform;
}

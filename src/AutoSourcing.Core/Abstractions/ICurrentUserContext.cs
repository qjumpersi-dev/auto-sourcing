namespace AutoSourcing.Core.Abstractions;

// Identifies who is acting on the current request.
//
// UserId is null when there is no signed-in user - background jobs and server-to-server API-key
// calls. In that case user filtering is disabled, so those paths still work across all data.
public interface ICurrentUserContext
{
    int? UserId { get; }

    bool IsAdmin { get; }
}

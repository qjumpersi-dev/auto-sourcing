using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.API.Auth;

// Bridges the request's signed-in user to the data layer's per-user filtering.
public class CurrentUserContext : ICurrentUserContext
{
    private readonly ICurrentUser _currentUser;

    public CurrentUserContext(ICurrentUser currentUser) => _currentUser = currentUser;

    public int? UserId => _currentUser.User?.Id;

    public bool IsAdmin => _currentUser.User?.Role == UserRole.Admin;
}

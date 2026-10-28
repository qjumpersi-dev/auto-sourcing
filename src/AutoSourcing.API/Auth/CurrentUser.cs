using AutoSourcing.Core.Entities;

namespace AutoSourcing.API.Auth;

public interface ICurrentUser
{
    User? User { get; }
    int? UserId { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public User? User => _accessor.HttpContext?.Items[AuthConstants.CurrentUserItem] as User;

    public int? UserId => User?.Id;
}

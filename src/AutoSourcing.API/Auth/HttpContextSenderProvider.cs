using AutoSourcing.Services.Auth;
using AutoSourcing.Services.Email;

namespace AutoSourcing.API.Auth;

// Builds the sending identity from the signed-in user, falling back to the platform default.
public class HttpContextSenderProvider : ISenderProvider
{
    private readonly ICurrentUser _currentUser;

    public HttpContextSenderProvider(ICurrentUser currentUser) => _currentUser = currentUser;

    public SenderIdentity Current
    {
        get
        {
            var user = _currentUser.User;
            if (user is null)
            {
                return SenderIdentity.Platform;
            }

            return new SenderIdentity(
                user.Id,
                user.SendFromAddress,
                string.IsNullOrWhiteSpace(user.SendFromName) ? user.DisplayName : user.SendFromName,
                string.IsNullOrWhiteSpace(user.ReplyToAddress) ? user.Email : user.ReplyToAddress);
        }
    }
}

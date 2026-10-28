using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Services.Microsoft;

public interface IMicrosoftTokenService
{
    Task<MicrosoftAccount> ConnectAsync(User user, string code, CancellationToken cancellationToken = default);
    Task<string> GetAccessTokenAsync(User user, CancellationToken cancellationToken = default);
    void Disconnect(User user);
}

public class MicrosoftTokenService : IMicrosoftTokenService
{
    private readonly IMicrosoftGraphService _graphService;
    private readonly ITokenProtector _tokenProtector;
    private readonly AutoSourcingDbContext _dbContext;

    public MicrosoftTokenService(IMicrosoftGraphService graphService, ITokenProtector tokenProtector, AutoSourcingDbContext dbContext)
    {
        _graphService = graphService;
        _tokenProtector = tokenProtector;
        _dbContext = dbContext;
    }

    public async Task<MicrosoftAccount> ConnectAsync(User user, string code, CancellationToken cancellationToken = default)
    {
        var tokens = await _graphService.ExchangeCodeAsync(code, cancellationToken);
        var account = await _graphService.GetAccountAsync(tokens.AccessToken, cancellationToken);

        user.MicrosoftAccountEmail = account.Email;
        user.MicrosoftAccessToken = _tokenProtector.Protect(tokens.AccessToken);
        user.MicrosoftAccessTokenExpiresAt = tokens.ExpiresAt;
        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            user.MicrosoftRefreshToken = _tokenProtector.Protect(tokens.RefreshToken);
        }

        user.MicrosoftConnectedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<string> GetAccessTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(user.MicrosoftAccessToken) &&
            user.MicrosoftAccessTokenExpiresAt is { } expiresAt &&
            expiresAt > DateTime.UtcNow)
        {
            return _tokenProtector.Unprotect(user.MicrosoftAccessToken);
        }

        if (string.IsNullOrWhiteSpace(user.MicrosoftRefreshToken))
        {
            throw new InvalidOperationException("This user has not connected a Microsoft 365 account.");
        }

        var refreshToken = _tokenProtector.Unprotect(user.MicrosoftRefreshToken);
        var tokens = await _graphService.RefreshAsync(refreshToken, cancellationToken);

        user.MicrosoftAccessToken = _tokenProtector.Protect(tokens.AccessToken);
        user.MicrosoftAccessTokenExpiresAt = tokens.ExpiresAt;
        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            user.MicrosoftRefreshToken = _tokenProtector.Protect(tokens.RefreshToken);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return tokens.AccessToken;
    }

    public void Disconnect(User user)
    {
        user.MicrosoftAccountEmail = null;
        user.MicrosoftRefreshToken = null;
        user.MicrosoftAccessToken = null;
        user.MicrosoftAccessTokenExpiresAt = null;
        user.MicrosoftConnectedAt = null;
        user.UpdatedAt = DateTime.UtcNow;
    }
}

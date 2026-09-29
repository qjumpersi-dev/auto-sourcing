using System.Security.Cryptography;
using System.Text;
using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Services.Auth;

public record AuthResult(bool Success, string? Token, User? User, string? Error);

public interface IAuthService
{
    Task<bool> HasAnyUserAsync(CancellationToken cancellationToken = default);
    Task<AuthResult> CreateFirstAdminAsync(string email, string displayName, string password, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<User?> ResolveAsync(string token, CancellationToken cancellationToken = default);
    Task LogoutAsync(string token, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(14);

    private readonly AutoSourcingDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(AutoSourcingDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public Task<bool> HasAnyUserAsync(CancellationToken cancellationToken = default)
        => _dbContext.Users.AnyAsync(cancellationToken);

    public async Task<AuthResult> CreateFirstAdminAsync(string email, string displayName, string password, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Users.AnyAsync(cancellationToken))
        {
            return new AuthResult(false, null, null, "An account already exists. Please sign in.");
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthResult(false, null, null, "Email and password are required.");
        }

        var user = new User
        {
            Email = NormalizeEmail(email),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? NormalizeEmail(email) : displayName.Trim(),
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.Admin,
            IsActive = true
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await IssueSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return new AuthResult(false, null, null, "Invalid email or password.");
        }

        return await IssueSessionAsync(user, cancellationToken);
    }

    public async Task<User?> ResolveAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var hash = HashToken(token);
        var session = await _dbContext.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);

        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= DateTime.UtcNow || !session.User.IsActive)
        {
            return null;
        }

        return session.User;
    }

    public async Task LogoutAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var hash = HashToken(token);
        var session = await _dbContext.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);
        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthResult> IssueSessionAsync(User user, CancellationToken cancellationToken)
    {
        var token = GenerateToken();

        _dbContext.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.Add(SessionLifetime)
        });

        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(true, token, user, null);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

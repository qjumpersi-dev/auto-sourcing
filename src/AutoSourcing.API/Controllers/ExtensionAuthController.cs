using System.Collections.Concurrent;
using System.Security.Cryptography;
using AutoSourcing.API.Auth;
using AutoSourcing.Data;
using AutoSourcing.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class ExtensionExchangeRequest
{
    public string Code { get; set; } = string.Empty;
}

// Lets the browser extension authenticate as the signed-in user without sharing the API key.
// The user generates a short code in the app, pastes it into the extension, and the extension
// exchanges it for a long-lived token.
[ApiController]
[Route("api/auth/extension")]
public class ExtensionAuthController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, (int UserId, DateTime ExpiresAt)> Codes = new();
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(365);
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;
    private readonly AutoSourcingDbContext _dbContext;

    public ExtensionAuthController(IAuthService authService, ICurrentUser currentUser, AutoSourcingDbContext dbContext)
    {
        _authService = authService;
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    [HttpPost("code")]
    public ActionResult CreateCode()
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        Prune();

        var code = GenerateCode();
        Codes[code] = (user.Id, DateTime.UtcNow.Add(CodeLifetime));

        return Ok(new { code, expiresInMinutes = (int)CodeLifetime.TotalMinutes });
    }

    [HttpPost("exchange")]
    public async Task<ActionResult> Exchange([FromBody] ExtensionExchangeRequest request, CancellationToken cancellationToken)
    {
        Prune();

        var code = request?.Code?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || !Codes.TryRemove(code, out var entry))
        {
            return BadRequest(new { error = "Invalid or expired code." });
        }

        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { error = "That code has expired. Generate a new one." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == entry.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return BadRequest(new { error = "Account not found." });
        }

        var token = await _authService.IssueTokenAsync(user, TokenLifetime, cancellationToken);

        return Ok(new
        {
            token,
            user = new { user.Id, user.Email, user.DisplayName }
        });
    }

    private static void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in Codes.Where(k => k.Value.ExpiresAt < now).ToList())
        {
            Codes.TryRemove(entry.Key, out _);
        }
    }

    private static string GenerateCode()
    {
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }
}

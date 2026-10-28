using AutoSourcing.API.Auth;
using AutoSourcing.Core.Entities;
using AutoSourcing.Data;
using AutoSourcing.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SetupRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateAccountRequest
{
    public string? DisplayName { get; set; }
    public string? SendFromAddress { get; set; }
    public string? SendFromName { get; set; }
    public string? ReplyToAddress { get; set; }
}

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;
    private readonly AutoSourcingDbContext _dbContext;

    public AuthController(IAuthService authService, ICurrentUser currentUser, AutoSourcingDbContext dbContext)
    {
        _authService = authService;
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    [HttpGet("status")]
    public async Task<ActionResult> Status(CancellationToken cancellationToken)
    {
        var hasUsers = await _authService.HasAnyUserAsync(cancellationToken);
        return Ok(new { hasUsers });
    }

    [HttpPost("setup")]
    public async Task<ActionResult> Setup([FromBody] SetupRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.CreateFirstAdminAsync(request.Email, request.DisplayName, request.Password, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { token = result.Token, user = ToDto(result.User!) });
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(new { token = result.Token, user = ToDto(result.User!) });
    }

    [HttpGet("me")]
    public ActionResult Me()
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        return Ok(ToDto(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult> UpdateMe([FromBody] UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        var tracked = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        if (tracked is null)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            tracked.DisplayName = request.DisplayName.Trim();
        }

        tracked.SendFromAddress = Normalize(request.SendFromAddress);
        tracked.SendFromName = Normalize(request.SendFromName);
        tracked.ReplyToAddress = Normalize(request.ReplyToAddress);
        tracked.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(tracked));
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var token = AuthConstants.ExtractBearerToken(Request);
        await _authService.LogoutAsync(token ?? string.Empty, cancellationToken);
        return Ok(new { signedOut = true });
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static object ToDto(User user) => new
    {
        id = user.Id,
        email = user.Email,
        displayName = user.DisplayName,
        role = user.Role.ToString(),
        sendFromAddress = user.SendFromAddress,
        sendFromName = user.SendFromName,
        replyToAddress = user.ReplyToAddress
    };
}

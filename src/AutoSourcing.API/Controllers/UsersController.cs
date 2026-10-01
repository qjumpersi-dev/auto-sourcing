using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.API.Controllers;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Recruiter";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserContext _currentUser;

    public UsersController(AutoSourcingDbContext dbContext, IPasswordHasher passwordHasher, ICurrentUserContext currentUser)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult> List(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Only administrators can manage users." });
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                Role = u.Role.ToString(),
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt,
                MicrosoftConnected = u.MicrosoftRefreshToken != null
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Only administrators can manage users." });
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return BadRequest(new { error = "A valid email address is required." });
        }

        if (password.Length < 8)
        {
            return BadRequest(new { error = "The password must be at least 8 characters." });
        }

        if (await _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return BadRequest(new { error = "A user with that email address already exists." });
        }

        var role = Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var parsed) ? parsed : UserRole.Recruiter;

        var user = new User
        {
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
            PasswordHash = _passwordHasher.Hash(password),
            Role = role,
            IsActive = true
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { user.Id, user.Email, user.DisplayName, Role = user.Role.ToString() });
    }

    // Any signed-in user can change their own password.
    [HttpPost("me/password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        if (!_passwordHasher.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            return BadRequest(new { error = "Your current password is incorrect." });
        }

        if ((request.NewPassword ?? string.Empty).Length < 8)
        {
            return BadRequest(new { error = "The new password must be at least 8 characters." });
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword!);
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { changed = true });
    }
}

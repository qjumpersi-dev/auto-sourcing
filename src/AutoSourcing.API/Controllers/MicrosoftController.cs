using AutoSourcing.API.Auth;
using AutoSourcing.Data;
using AutoSourcing.Services.Microsoft;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/auth/microsoft")]
public class MicrosoftController : ControllerBase
{
    private readonly IMicrosoftGraphService _graphService;
    private readonly IMicrosoftTokenService _tokenService;
    private readonly ITokenProtector _tokenProtector;
    private readonly ICurrentUser _currentUser;
    private readonly AutoSourcingDbContext _dbContext;
    private readonly MicrosoftOptions _options;

    public MicrosoftController(
        IMicrosoftGraphService graphService,
        IMicrosoftTokenService tokenService,
        ITokenProtector tokenProtector,
        ICurrentUser currentUser,
        AutoSourcingDbContext dbContext,
        IOptions<MicrosoftOptions> options)
    {
        _graphService = graphService;
        _tokenService = tokenService;
        _tokenProtector = tokenProtector;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _options = options.Value;
    }

    [HttpGet("status")]
    public ActionResult Status()
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        return Ok(new
        {
            connected = !string.IsNullOrWhiteSpace(user.MicrosoftRefreshToken),
            accountEmail = user.MicrosoftAccountEmail,
            connectedAt = user.MicrosoftConnectedAt,
            configured = !string.IsNullOrWhiteSpace(_options.ClientId) && !string.IsNullOrWhiteSpace(_options.RedirectUri)
        });
    }

    // Returns the Microsoft sign-in URL. The client fetches this with its bearer token, then navigates to it.
    [HttpGet("connect-url")]
    public ActionResult ConnectUrl()
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            return BadRequest(new { error = "Microsoft 365 is not configured on the server (missing ClientId/RedirectUri)." });
        }

        var state = _tokenProtector.Protect($"{user.Id}:{Guid.NewGuid()}");
        return Ok(new { url = _graphService.BuildAuthorizeUrl(state) });
    }

    // Microsoft redirects the browser here. Public endpoint: the user is identified by the signed state.
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Redirect(BuildRedirect($"microsoft=error&reason={Uri.EscapeDataString(errorDescription ?? error)}"));
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Redirect(BuildRedirect("microsoft=error&reason=missing_code_or_state"));
        }

        int userId;
        try
        {
            var decoded = _tokenProtector.Unprotect(state);
            userId = int.Parse(decoded.Split(':', 2)[0]);
        }
        catch
        {
            return Redirect(BuildRedirect("microsoft=error&reason=invalid_state"));
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return Redirect(BuildRedirect("microsoft=error&reason=unknown_user"));
        }

        try
        {
            await _tokenService.ConnectAsync(user, code, cancellationToken);
            return Redirect(BuildRedirect("microsoft=connected"));
        }
        catch (Exception ex)
        {
            return Redirect(BuildRedirect($"microsoft=error&reason={Uri.EscapeDataString(ex.Message)}"));
        }
    }

    [HttpPost("disconnect")]
    public async Task<ActionResult> Disconnect(CancellationToken cancellationToken)
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

        _tokenService.Disconnect(tracked);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { connected = false });
    }

    private string BuildRedirect(string query)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.AppBaseUrl) ? "http://localhost:5173" : _options.AppBaseUrl.TrimEnd('/');
        return $"{baseUrl}/?{query}";
    }
}

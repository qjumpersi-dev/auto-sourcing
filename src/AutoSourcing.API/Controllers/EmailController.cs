using AutoSourcing.API.Auth;
using AutoSourcing.Services.Auth;
using AutoSourcing.Services.Email;
using Microsoft.AspNetCore.Mvc;

namespace AutoSourcing.API.Controllers;

public class TestEmailRequest
{
    public string? To { get; set; }
}

[ApiController]
[Route("api/email")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ISenderProvider _senderProvider;
    private readonly ICurrentUser _currentUser;

    public EmailController(IEmailService emailService, ISenderProvider senderProvider, ICurrentUser currentUser)
    {
        _emailService = emailService;
        _senderProvider = senderProvider;
        _currentUser = currentUser;
    }

    [HttpPost("test")]
    public async Task<ActionResult> SendTest([FromBody] TestEmailRequest? request, CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return Unauthorized(new { error = "Not signed in." });
        }

        var to = string.IsNullOrWhiteSpace(request?.To) ? user.Email : request!.To!.Trim();
        var sender = _senderProvider.Current;

        var body =
            "<p>This is a test email from the QJumpers AI Sourcing app.</p>" +
            "<p>If you received it, your email sending is configured correctly.</p>" +
            $"<p>Sent on behalf of: <strong>{System.Net.WebUtility.HtmlEncode(user.DisplayName)}</strong> " +
            $"&lt;{System.Net.WebUtility.HtmlEncode(user.Email)}&gt;</p>";

        try
        {
            await _emailService.SendAsync(
                new[] { to },
                "Test email from AI Sourcing",
                body,
                headers: null,
                cancellationToken: cancellationToken,
                sender: sender);

            return Ok(new { sent = true, to });
        }
        catch (Exception ex)
        {
            return Ok(new { sent = false, to, error = Describe(ex) });
        }
    }

    private static string Describe(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message) && !messages.Contains(current.Message))
            {
                messages.Add(current.Message);
            }
        }

        return messages.Count == 0 ? "Unknown error." : string.Join(" | ", messages);
    }
}

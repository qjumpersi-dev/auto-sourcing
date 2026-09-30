using AutoSourcing.Data;
using AutoSourcing.Services.Microsoft;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Services.Email;

// Routes outgoing email: sends via Microsoft 365 (Graph) when the sender has connected their
// account, otherwise falls back to the platform SMTP transport.
public class EmailDispatcher : IEmailService
{
    private readonly ISmtpMailSender _smtpMailSender;
    private readonly IMicrosoftGraphService _graphService;
    private readonly IMicrosoftTokenService _tokenService;
    private readonly AutoSourcingDbContext _dbContext;

    public EmailDispatcher(
        ISmtpMailSender smtpMailSender,
        IMicrosoftGraphService graphService,
        IMicrosoftTokenService tokenService,
        AutoSourcingDbContext dbContext)
    {
        _smtpMailSender = smtpMailSender;
        _graphService = graphService;
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    public async Task SendAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default,
        SenderIdentity? sender = null)
    {
        var recipients = to
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("No recipient email addresses were provided.");
        }

        if (sender?.UserId is int userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is not null && !string.IsNullOrWhiteSpace(user.MicrosoftRefreshToken))
            {
                var accessToken = await _tokenService.GetAccessTokenAsync(user, cancellationToken);

                foreach (var recipient in recipients)
                {
                    await _graphService.SendMailAsync(
                        accessToken,
                        new MicrosoftSendMail(recipient, subject, body, sender.ReplyTo, sender.FromAddress, sender.FromName),
                        cancellationToken);
                }

                return;
            }
        }

        await _smtpMailSender.SendAsync(recipients, subject, body, headers, cancellationToken, sender);
    }
}

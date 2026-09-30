using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Email;

public class SmtpEmailService : ISmtpMailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
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

        var fromAddress = string.IsNullOrWhiteSpace(sender?.FromAddress) ? _options.FromAddress : sender!.FromAddress!;
        var fromName = string.IsNullOrWhiteSpace(sender?.FromName) ? _options.FromName : sender!.FromName!;

        using var message = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(fromName)
                ? new MailAddress(fromAddress)
                : new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(new MailAddress(recipient));
        }

        if (!string.IsNullOrWhiteSpace(sender?.ReplyTo))
        {
            message.ReplyToList.Add(new MailAddress(sender!.ReplyTo!));
        }

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                message.Headers[key] = value;
            }
        }

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            Timeout = _options.TimeoutMs
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}

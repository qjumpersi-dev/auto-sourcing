using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;

    public SmtpEmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(IEnumerable<string> to, string subject, string body, IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
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

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(new MailAddress(recipient));
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

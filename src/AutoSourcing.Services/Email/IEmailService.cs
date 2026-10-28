namespace AutoSourcing.Services.Email;

// Low-level SMTP transport. Use IEmailService for sending; it routes to Graph or SMTP.
public interface ISmtpMailSender
{
    Task SendAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default,
        SenderIdentity? sender = null);
}

public interface IEmailService
{
    Task SendAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default,
        SenderIdentity? sender = null);
}

namespace AutoSourcing.Services.Email;

public interface IEmailService
{
    Task SendAsync(IEnumerable<string> to, string subject, string body, IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
}

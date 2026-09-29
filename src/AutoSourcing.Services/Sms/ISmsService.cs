namespace AutoSourcing.Services.Sms;

public record SmsSendResult(bool Sent, string Channel, string? Error);

public interface ISmsService
{
    Task<SmsSendResult> SendAsync(string to, string message, CancellationToken cancellationToken = default);
}

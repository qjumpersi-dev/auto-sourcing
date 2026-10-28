namespace AutoSourcing.Services.Sms;

public record SmsSendResult(bool Sent, string Channel, string? Error);

public interface ISmsService
{
    // messageId (when known) is used to receive Twilio delivery status callbacks.
    Task<SmsSendResult> SendAsync(string to, string message, int? messageId = null, CancellationToken cancellationToken = default);
}

namespace AutoSourcing.Services.Sms;

public class SmsOptions
{
    public const string SectionName = "Sms";

    public string? TwilioAccountSid { get; set; }
    public string? TwilioAuthToken { get; set; }
    public string? FromNumber { get; set; }

    // Public base URL of the API, used for the Twilio delivery-status callback.
    public string? PublicBaseUrl { get; set; }

    // Legacy RCS attempt - disabled; Twilio's Messages API does not support RCS this way.
    public bool UseRcsFallback { get; set; } = false;
}

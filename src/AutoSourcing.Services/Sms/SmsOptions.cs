namespace AutoSourcing.Services.Sms;

public class SmsOptions
{
    public const string SectionName = "Sms";

    public string? TwilioAccountSid { get; set; }
    public string? TwilioAuthToken { get; set; }
    public string? FromNumber { get; set; }
    public bool UseRcsFallback { get; set; } = true;
}

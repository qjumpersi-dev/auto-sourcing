using AutoSourcing.Core.Entities;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Email;

public interface IUnsubscribeService
{
    string AppendFooter(string body, Lead lead);
    IReadOnlyDictionary<string, string> BuildHeaders(Lead lead);
}

public class UnsubscribeService : IUnsubscribeService
{
    private readonly EmailOptions _options;

    public UnsubscribeService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public string AppendFooter(string body, Lead lead)
    {
        var unsubscribeUrl = BuildUnsubscribeUrl(lead);
        var consentUrl = BuildConsentUrl(lead);
        var agentUrl = BuildAgentUrl(lead);
        var footer =
            "<div style=\"margin-top:24px;padding-top:12px;border-top:1px solid #e5e7eb;font-size:12px;color:#6b7280;\">" +
            "<p style=\"margin-bottom:8px;\">" +
            "Want to get updates via SMS? <a href=\"" + consentUrl + "\" style=\"color:#2563eb;text-decoration:underline;\">Confirm here to opt in to text messages</a> from QJumpers US C Corp about job opportunities, interview scheduling, and recruitment updates. " +
            "Message frequency varies. Message and data rates may apply. Reply HELP for help or STOP to cancel." +
            "</p>" +
            "<p style=\"margin-bottom:8px;\">" +
            "Have questions about the role or the company? <a href=\"" + agentUrl + "\" style=\"color:#2563eb;text-decoration:underline;\">Chat with our AI assistant</a>." +
            "</p>" +
            "<p>" +
            "You are receiving this email because we believe you may be interested in recruitment solutions. " +
            "<a href=\"" + unsubscribeUrl + "\" style=\"color:#6b7280;text-decoration:underline;\">Unsubscribe</a>" +
            "</p>" +
            "</div>";

        return body + footer;
    }

    public IReadOnlyDictionary<string, string> BuildHeaders(Lead lead)
    {
        var url = BuildUnsubscribeUrl(lead);
        return new Dictionary<string, string>
        {
            ["List-Unsubscribe"] = $"<{url}>",
            ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
        };
    }

    private string BuildUnsubscribeUrl(Lead lead)
    {
        var baseUrl = (_options.PublicBaseUrl ?? string.Empty).TrimEnd('/');
        return $"{baseUrl}/api/unsubscribe/{lead.Id}";
    }

    private string BuildConsentUrl(Lead lead)
    {
        var baseUrl = (_options.PublicBaseUrl ?? string.Empty).TrimEnd('/');
        return $"{baseUrl}/api/consent/{lead.Id}";
    }

    private string BuildAgentUrl(Lead lead)
    {
        var baseUrl = (_options.PublicBaseUrl ?? string.Empty).TrimEnd('/');
        return $"{baseUrl}/api/agent/{lead.Id}";
    }
}

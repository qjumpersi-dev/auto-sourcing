using System.Security.Cryptography;
using System.Text;
using AutoSourcing.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Email;

public interface IUnsubscribeService
{
    string AppendFooter(string body, Lead lead);
    IReadOnlyDictionary<string, string> BuildHeaders(Lead lead);
    string BuildUnsubscribeUrl(int leadId);
    bool ValidateToken(int leadId, string? token);
}

public class UnsubscribeService : IUnsubscribeService
{
    private readonly EmailOptions _options;
    private readonly IConfiguration _configuration;

    public UnsubscribeService(IOptions<EmailOptions> options, IConfiguration configuration)
    {
        _options = options.Value;
        _configuration = configuration;
    }

    public string AppendFooter(string body, Lead lead)
    {
        var unsubscribeUrl = BuildUnsubscribeUrl(lead.Id);
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
        var url = BuildUnsubscribeUrl(lead.Id);
        return new Dictionary<string, string>
        {
            ["List-Unsubscribe"] = $"<{url}>",
            ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
        };
    }

    // Signed so the lead id alone can't be used to unsubscribe someone.
    public string BuildUnsubscribeUrl(int leadId)
    {
        var token = SignedToken.Create("unsubscribe", leadId.ToString(), SigningKey);
        return $"{Base}/api/unsubscribe/{leadId}/{token}";
    }

    public bool ValidateToken(int leadId, string? token)
        => SignedToken.Verify("unsubscribe", leadId.ToString(), token, SigningKey);

    private byte[] SigningKey
    {
        get
        {
            var secret = !string.IsNullOrWhiteSpace(_options.SigningKey)
                ? _options.SigningKey
                : _configuration["ApiKey"] ?? "autosourcing-dev-signing-key";

            return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        }
    }

    private string BuildConsentUrl(Lead lead) => $"{Base}/api/consent/{lead.Id}";

    private string BuildAgentUrl(Lead lead) => $"{Base}/api/agent/{lead.Id}";

    private string Base => (_options.PublicBaseUrl ?? string.Empty).TrimEnd('/');
}

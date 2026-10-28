using AutoSourcing.Core.Entities;
using AutoSourcing.Services.Email;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Outreach;

public class PersonalizationService : IPersonalizationService
{
    private const string PersonalisationPrefix = "{{Personalisation:";
    private readonly EmailOptions _emailOptions;

    public PersonalizationService(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    private static readonly IReadOnlyList<PersonalisationOption> Options =
    [
        new(
            "hiring-volume",
            "High-volume hiring",
            "Best when the company hires many people into the same role.",
            "{{Company}} hires a lot of {{JobTitle}}s. The QJumpers AI Talent Sourcing Engine has one of the largest candidate pools in the world for these positions with contact details and AI Augmented skills. Larger than LinkedIn, Seek and Indeed."),
        new(
            "specialised-skills",
            "Specialised skill sets",
            "Best when the company hires for niche or hard-to-find skills.",
            "{{Company}} often hires for a lot of specialised skill sets. The QJumpers AI Talent Sourcing Engine has one of the largest candidate pools in the world for these positions with contact details and AI Augmented skills. Larger than LinkedIn, Seek and Indeed.")
    ];

    public IReadOnlyList<PersonalisationOption> GetOptions() => Options;

    public string RenderTemplate(string template, Lead lead, string? jobUrl = null, string? orgName = null, string? jobLocation = null)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        var expanded = ExpandPersonalisations(template);

        var replacements = new Dictionary<string, string>
        {
            ["{{FirstName}}"] = lead.FirstName,
            ["{{LastName}}"] = lead.LastName,
            ["{{FullName}}"] = $"{lead.FirstName} {lead.LastName}".Trim(),
            ["{{Email}}"] = lead.Email,
            ["{{Company}}"] = !string.IsNullOrWhiteSpace(orgName) ? orgName : (lead.Company ?? "your company"),
            ["{{LeadCompany}}"] = lead.Company ?? string.Empty,
            ["{{JobTitle}}"] = lead.JobTitle ?? string.Empty,
            ["{{Location}}"] = jobLocation ?? string.Empty,
            ["{{LeadLocation}}"] = lead.Location ?? string.Empty,
            ["{{JobUrl}}"] = jobUrl ?? string.Empty,
            ["{{OrgName}}"] = orgName ?? string.Empty,
            ["{{ConsentUrl}}"] = $"{_emailOptions.PublicBaseUrl?.TrimEnd('/')}/api/consent/{lead.Id}"
        };

        return replacements.Aggregate(expanded, (current, pair) =>
            current.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase));
    }

    private static string ExpandPersonalisations(string template)
    {
        var result = template;
        foreach (var option in Options)
        {
            var token = PersonalisationPrefix + option.Key + "}}";
            result = result.Replace(token, option.Template, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}

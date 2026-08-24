using AutoSourcing.Core.Entities;

namespace AutoSourcing.Services.Outreach;

public class PersonalizationService : IPersonalizationService
{
    public string RenderTemplate(string template, Lead lead)
    {
        var replacements = new Dictionary<string, string>
        {
            ["{{FirstName}}"] = lead.FirstName,
            ["{{LastName}}"] = lead.LastName,
            ["{{FullName}}"] = $"{lead.FirstName} {lead.LastName}".Trim(),
            ["{{Email}}"] = lead.Email,
            ["{{Company}}"] = lead.Company ?? "your company",
            ["{{JobTitle}}"] = lead.JobTitle ?? string.Empty
        };

        return replacements.Aggregate(template, (current, pair) =>
            current.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase));
    }
}

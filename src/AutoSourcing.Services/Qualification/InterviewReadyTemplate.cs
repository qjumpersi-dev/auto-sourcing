using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Services.Qualification;

public record InterviewReadyCheckTemplate(InterviewReadyCriterion Criterion, string Prompt, string? Expected);

// Builds the fixed 11 interview-ready checks, filling in the detail from the job's Role brief so the
// same information is reused by the agent, the checklist and the handover.
public static class InterviewReadyTemplate
{
    public static IReadOnlyList<InterviewReadyCheckTemplate> Build(Job? job)
    {
        return
        [
            new(InterviewReadyCriterion.RoleRelevance,
                "Do you meet the essential requirements for this role?",
                Trim(Join(job?.MustHaves, job?.Skills, job?.Education))),
            new(InterviewReadyCriterion.RoleUnderstanding,
                "Have you reviewed the role summary?",
                Trim(Join(job?.RoleSummary, job?.AdvertCopy))),
            new(InterviewReadyCriterion.WorkHours,
                "Are the shift pattern and working hours acceptable?",
                Join(job?.ShiftPattern, job?.WorkingHours, job?.OnCallRequired == true ? "on-call required" : null)),
            new(InterviewReadyCriterion.Location,
                "Is the work location, commute, travel or relocation acceptable?",
                Join(job?.Location, job?.TravelRequirement, job?.RelocationRequired == true ? "relocation required" : null)),
            new(InterviewReadyCriterion.Pay,
                "Is the pay range acceptable?",
                Pay(job)),
            new(InterviewReadyCriterion.WorkAuthorisation,
                "Do you have the right to work in the role's country, or is sponsorship available?",
                Join(job?.WorkAuthorisationRequired == true ? "right to work required" : null,
                    job?.SponsorshipAvailable == true ? "sponsorship available" : null)),
            new(InterviewReadyCriterion.BackgroundChecks,
                "Are you comfortable with the client's background, security or drug-screening requirements?",
                Trim(job?.BackgroundChecks)),
            new(InterviewReadyCriterion.Licences,
                "Do you hold the required licences and certifications?",
                Trim(job?.RequiredLicences)),
            new(InterviewReadyCriterion.Interest,
                "Are you genuinely interested and ready to proceed to the client interview?",
                null),
            new(InterviewReadyCriterion.HumanConfirmation,
                "QJumpers specialist has completed the confirmation and verified the record.",
                null),
            new(InterviewReadyCriterion.InterviewArranged,
                "Interview booked and joining details sent to the candidate.",
                null)
        ];
    }

    private static string? Join(params string?[] values)
    {
        var parts = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
        var joined = string.Join(" · ", parts);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    private static string? Pay(Job? job)
    {
        if (job is null)
        {
            return null;
        }

        var amount = job.SalaryFrom is null && job.SalaryTo is null
            ? null
            : job.SalaryFrom is { } from && job.SalaryTo is { } to
                ? $"{from:N0} - {to:N0}"
                : job.SalaryFrom is { } only
                    ? $"from {only:N0}"
                    : $"up to {job.SalaryTo:N0}";

        return Join(job.PayCurrency, amount, job.PayPeriod.ToString(), job.SalaryNotes);
    }

    private static string? Trim(string? value, int max = 400)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        return text.Length <= max ? text : text[..max] + "...";
    }
}
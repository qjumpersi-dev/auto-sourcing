namespace AutoSourcing.Services.Rhetorik;

public record EligibleEmail(string Address, string Type, bool IsVerified);

public static class RhetorikEmailSelector
{
    public const string PersonalType = "Personal";
    public const string BusinessType = "Business";

    public static IReadOnlyList<EligibleEmail> SelectEligible(
        IReadOnlyList<RhetorikContactEmail>? contactEmails,
        IReadOnlyList<RhetorikProfileEmail>? profileEmails)
    {
        var contact = contactEmails ?? [];

        var personal = contact
            .Where(e => IsPersonal(e) && HasAddress(e.Email))
            .OrderBy(e => e.Priority ?? int.MaxValue)
            .Select(e => new EligibleEmail(e.Email!.Trim(), PersonalType, IsVerifiedStatus(e.Status)))
            .ToList();

        var verifiedBusiness = contact
            .Where(e => IsBusiness(e) && IsVerifiedStatus(e.Status) && HasAddress(e.Email))
            .OrderBy(e => e.Priority ?? int.MaxValue)
            .Select(e => new EligibleEmail(e.Email!.Trim(), BusinessType, true))
            .ToList();

        var result = new List<EligibleEmail>();
        result.AddRange(personal);

        if (personal.Count == 0)
        {
            var fallback = (profileEmails ?? [])
                .Where(p => HasAddress(p.Email))
                .OrderBy(p => p.Priority ?? int.MaxValue)
                .FirstOrDefault();

            if (fallback is not null)
            {
                result.Add(new EligibleEmail(fallback.Email!.Trim(), PersonalType, false));
            }
        }

        result.AddRange(verifiedBusiness);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return result.Where(e => seen.Add(e.Address)).ToList();
    }

    public static bool IsPersonal(RhetorikContactEmail email) =>
        string.Equals(email.Type, PersonalType, StringComparison.OrdinalIgnoreCase);

    public static bool IsBusiness(RhetorikContactEmail email) =>
        string.Equals(email.Type, BusinessType, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(email.Type, "Professional", StringComparison.OrdinalIgnoreCase);

    public static bool IsVerifiedStatus(string? status) =>
        string.Equals(status, "Verified", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Valid", StringComparison.OrdinalIgnoreCase);

    private static bool HasAddress(string? email) => !string.IsNullOrWhiteSpace(email);
}
using System.Text.Json.Serialization;

namespace AutoSourcing.Services.Rhetorik;

public class ProfileSearchRequest
{
    public List<string> ProfileIds { get; set; } = [];
    public List<string> Keywords { get; set; } = [];
    public List<string> JobTitles { get; set; } = [];
    public string JobTitleScope { get; set; } = "any";
    public List<string> Companies { get; set; } = [];
    public string CompanyScope { get; set; } = "current";
    public List<string> Expertises { get; set; } = [];
    public string ExpertiseMode { get; set; } = "must_have_any";
    public List<string> Countries { get; set; } = [];
    public List<string> States { get; set; } = [];
    public List<string> Cities { get; set; } = [];
    public List<string> JobTitleSuggestions { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public int MaxResults { get; set; } = 500;

    public const string MustHaveAny = "must_have_any";
    public const string MustHaveAll = "must_have_all";
    public const string MustNotHaveAny = "must_not_have_any";
    public const string MustNotHaveAll = "must_not_have_all";

    public const string EmailTag = "Profile Has Email";

    internal Dictionary<string, object> BuildParameters()
    {
        var parameters = new Dictionary<string, object>();

        AddIsOneOf(parameters, ProfileIds, "profile_id");

        if (Keywords is { Count: > 0 })
        {
            parameters["keywords"] = new[] { new { @operator = "is one of", value = Keywords } };
        }

        AddIsOneOf(parameters, JobTitles, JobTitleScope switch
        {
            "current" => "current_job_titles",
            "past" => "previous_job_titles",
            _ => "job_titles"
        });

        AddIsOneOf(parameters, Companies, CompanyScope switch
        {
            "past" => "previous_company_names",
            _ => CompanyScope == "any" ? "company_names" : "current_company_names"
        });

        if (Expertises is { Count: > 0 })
        {
            parameters["expertises"] = ExpertiseMode switch
            {
                MustHaveAll => Expertises.Select(v => (object)new { @operator = "is", value = new[] { v } }).ToArray(),
                MustNotHaveAny => new object[] { new { @operator = "is not one of", value = Expertises } },
                MustNotHaveAll => Expertises.Select(v => (object)new { @operator = "is not", value = new[] { v } }).ToArray(),
                _ => new object[] { new { @operator = "is one of", value = Expertises } }
            };
        }

        AddIsOneOf(parameters, Countries, "countries");
        AddIsOneOf(parameters, States, "states");
        AddIsOneOf(parameters, Cities, "cities");

        parameters["profile_tags"] = new[] { new { @operator = "is one of", value = new[] { EmailTag } } };

        return parameters;
    }

    private static void AddIsOneOf(Dictionary<string, object> parameters, List<string>? values, string field)
    {
        if (values is { Count: > 0 })
        {
            parameters[field] = new[] { new { @operator = "is one of", value = values } };
        }
    }
}

public class ProfileSearchResponse
{
    [JsonPropertyName("counts")]
    public RhetorikCounts? Counts { get; set; }

    [JsonPropertyName("results")]
    public IReadOnlyList<RhetorikProfileResult> Results { get; set; } = [];

    [JsonPropertyName("pagination")]
    public RhetorikPagination? Pagination { get; set; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<RhetorikApiError>? Errors { get; set; }
}

public class RhetorikCounts
{
    [JsonPropertyName("profiles_total_results")]
    public int ProfilesTotalResults { get; set; }

    [JsonPropertyName("profiles_total_returned")]
    public int ProfilesTotalReturned { get; set; }
}

public class RhetorikPagination
{
    [JsonPropertyName("current")]
    public int Current { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }

    [JsonPropertyName("next_page")]
    public int? NextPage { get; set; }
}

public class RhetorikApiError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class RhetorikProfileResult
{
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("profile_data")]
    public RhetorikProfileData? ProfileData { get; set; }

    [JsonPropertyName("contact_data")]
    public RhetorikContactDataBlock? ContactData { get; set; }

    [JsonPropertyName("resume_data")]
    public RhetorikResumeData? ResumeData { get; set; }
}

public class RhetorikProfileData
{
    [JsonPropertyName("profile_id")]
    public string ProfileId { get; set; } = string.Empty;

    [JsonPropertyName("profile_first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("profile_last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("profile_headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("profile_summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("profile_expertises")]
    public IReadOnlyList<string>? Expertises { get; set; }

    [JsonPropertyName("profile_tags")]
    public IReadOnlyList<string>? Tags { get; set; }

    [JsonPropertyName("profile_address")]
    public RhetorikAddress? Address { get; set; }

    [JsonPropertyName("profile_educations")]
    public IReadOnlyList<RhetorikEducation>? Educations { get; set; }

    [JsonPropertyName("profile_certifications")]
    public IReadOnlyList<string>? Certifications { get; set; }

    [JsonPropertyName("profile_industries")]
    public IReadOnlyList<string>? Industries { get; set; }

    [JsonPropertyName("profile_languages")]
    public IReadOnlyList<string>? Languages { get; set; }

    [JsonPropertyName("profile_memberships")]
    public IReadOnlyList<string>? Memberships { get; set; }

    [JsonPropertyName("profile_publications")]
    public IReadOnlyList<string>? Publications { get; set; }

    [JsonPropertyName("profile_awards")]
    public IReadOnlyList<string>? Awards { get; set; }

    [JsonPropertyName("profile_patents")]
    public IReadOnlyList<string>? Patents { get; set; }

    [JsonPropertyName("profile_last_updated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("profile_emails")]
    public IReadOnlyList<RhetorikProfileEmail>? ProfileEmails { get; set; }
}

public class RhetorikEducation
{
    [JsonPropertyName("educational_establishment")]
    public string? EducationalEstablishment { get; set; }

    [JsonPropertyName("diploma")]
    public string? Diploma { get; set; }

    [JsonPropertyName("specialization")]
    public string? Specialization { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }
}

public class RhetorikProfileEmail
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("priority")]
    public int? Priority { get; set; }
}

public class RhetorikAddress
{
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }
}

public class RhetorikContactDataBlock
{
    [JsonPropertyName("contact_current_experiences")]
    public IReadOnlyList<RhetorikExperience>? CurrentExperiences { get; set; }

    [JsonPropertyName("contact_emails")]
    public IReadOnlyList<RhetorikContactEmail>? ContactEmails { get; set; }
}

public class RhetorikResumeData
{
    [JsonPropertyName("experiences")]
    public IReadOnlyList<RhetorikExperience>? Experiences { get; set; }

    [JsonPropertyName("educations")]
    public IReadOnlyList<RhetorikEducation>? Educations { get; set; }

    [JsonPropertyName("certifications")]
    public IReadOnlyList<RhetorikCertification>? Certifications { get; set; }

    [JsonPropertyName("memberships")]
    public IReadOnlyList<RhetorikMembership>? Memberships { get; set; }

    [JsonPropertyName("publications")]
    public IReadOnlyList<RhetorikPublication>? Publications { get; set; }

    [JsonPropertyName("patents")]
    public IReadOnlyList<RhetorikPatent>? Patents { get; set; }

    [JsonPropertyName("awards")]
    public IReadOnlyList<RhetorikAward>? Awards { get; set; }
}

public class RhetorikCertification
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("authority")]
    public string? Authority { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }
}

public class RhetorikMembership
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }
}

public class RhetorikPublication
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

public class RhetorikPatent
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

public class RhetorikAward
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

public record RhetorikContactEmailData(
    IReadOnlyList<RhetorikContactEmail> ContactEmails,
    IReadOnlyList<RhetorikProfileEmail> ProfileEmails);

public class RhetorikContactEmail
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("status_last_verification_date")]
    public string? StatusLastVerificationDate { get; set; }
}

public class RhetorikExperience
{
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }
    [JsonPropertyName("raw_company_name")]
    public string? RawCompanyName { get; set; }

    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("current")]
    public bool? Current { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }
}

public class AutocompleteResponse
{
    [JsonPropertyName("results")]
    public IReadOnlyList<AutocompleteSuggestion> Results { get; set; } = [];
}

public class AutocompleteSuggestion
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int? Count { get; set; }
}




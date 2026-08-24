using System.Text.Json.Serialization;

namespace AutoSourcing.Services.Rhetorik;

public class RhetorikSearchRequest
{
    public List<string>? Keywords { get; set; }
    public List<string>? JobTitles { get; set; }
    public List<string>? Companies { get; set; }
    public List<string>? Countries { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    internal Dictionary<string, object> BuildParameters()
    {
        var parameters = new Dictionary<string, object>();

        void Add(string field, List<string>? values)
        {
            if (values is { Count: > 0 })
            {
                parameters[field] = new[] { new { @operator = "includes", value = values } };
            }
        }

        Add("keywords", Keywords);
        Add("job_title", JobTitles);
        Add("company_name", Companies);
        Add("country", Countries);

        return parameters;
    }
}

public class RhetorikSearchResponse
{
    [JsonPropertyName("counts")]
    public RhetorikCounts? Counts { get; set; }

    [JsonPropertyName("results")]
    public IReadOnlyList<RhetorikContactResult> Results { get; set; } = [];

    [JsonPropertyName("pagination")]
    public RhetorikPagination? Pagination { get; set; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<RhetorikApiError>? Errors { get; set; }
}

public class RhetorikCounts
{
    [JsonPropertyName("contacts_total_results")]
    public int ContactsTotalResults { get; set; }

    [JsonPropertyName("contacts_total_returned")]
    public int ContactsTotalReturned { get; set; }
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

public class RhetorikContactResult
{
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("contact_data")]
    public RhetorikContactData? ContactData { get; set; }
}

public class RhetorikContactData
{
    [JsonPropertyName("contact_id")]
    public string ContactId { get; set; } = string.Empty;

    [JsonPropertyName("contact_first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("contact_last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("contact_emails")]
    public IReadOnlyList<RhetorikEmail>? Emails { get; set; }

    [JsonPropertyName("contact_phones")]
    public IReadOnlyList<RhetorikPhone>? Phones { get; set; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("contact_country")]
    public string? Country { get; set; }

    [JsonPropertyName("contact_social_links")]
    public IReadOnlyList<RhetorikSocialLink>? SocialLinks { get; set; }
}

public class RhetorikEmail
{
    [JsonPropertyName("email")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class RhetorikPhone
{
    [JsonPropertyName("phone")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class RhetorikSocialLink
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

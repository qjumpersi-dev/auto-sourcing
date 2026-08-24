namespace AutoSourcing.Services.Rhetorik;

public class RhetorikSearchRequest
{
    public string? Keywords { get; set; }
    public string? JobTitle { get; set; }
    public string? Company { get; set; }
    public string? Country { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class RhetorikSearchResponse
{
    public int TotalResults { get; set; }
    public IReadOnlyList<RhetorikProfile> Results { get; set; } = [];
}

public class RhetorikProfile
{
    public string ExternalId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? LinkedInUrl { get; set; }
}

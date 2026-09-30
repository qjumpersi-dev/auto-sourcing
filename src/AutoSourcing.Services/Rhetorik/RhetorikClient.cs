using System.Net.Http.Json;
using System.Text.Json;
using AutoSourcing.Core.Entities;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Rhetorik;

public class RhetorikClient : IRhetorikClient
{
    private const string ProfileSearchEndpoint = "profile/search";
    private const string AutocompleteEndpoint = "autocomplete";

    private const int MaxPageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly RhetorikOptions _options;

    public RhetorikClient(HttpClient httpClient, IOptions<RhetorikOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress ??= new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
    }

    public async Task<ProfileSearchResponse> SearchProfilesAsync(ProfileSearchRequest request, CancellationToken cancellationToken = default)
    {
        return await SearchAllPagesAsync(request, revealAllData: false, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, RhetorikContactEmailData>> FetchContactEmailsAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default)
    {
        // Contact-email discovery uses Rhetorik's paid "reveal_all_data". Never call it unless enabled.
        if (!_options.RevealContactEmails)
        {
            return new Dictionary<string, RhetorikContactEmailData>();
        }

        var ids = profileIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<string, RhetorikContactEmailData>();
        }

        var request = new ProfileSearchRequest
        {
            ProfileIds = ids,
            MaxResults = ids.Count
        };

        var response = await SearchAllPagesAsync(request, revealAllData: true, cancellationToken);

        return response.Results
            .Where(r => r.ProfileData is not null && !string.IsNullOrWhiteSpace(r.ProfileData.ProfileId))
            .ToDictionary(
                r => r.ProfileData!.ProfileId,
                r => new RhetorikContactEmailData(
                    r.ContactData?.ContactEmails ?? [],
                    r.ProfileData?.ProfileEmails ?? []));
    }

    private async Task<ProfileSearchResponse> SearchAllPagesAsync(ProfileSearchRequest request, bool revealAllData, CancellationToken cancellationToken)
    {
        var maxResults = Math.Clamp(request.MaxResults, 1, 1000);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var allResults = new List<RhetorikProfileResult>();
        ProfileSearchResponse? lastResponse = null;

        while (allResults.Count < maxResults)
        {
            var response = await SearchSinglePageAsync(request, pageNumber, pageSize, revealAllData, cancellationToken);
            lastResponse = response;

            allResults.AddRange(response.Results);

            var nextPage = response.Pagination?.NextPage;
            if (nextPage is null || nextPage <= pageNumber || pageNumber >= 100 || response.Results.Count == 0)
            {
                break;
            }

            if (response.Pagination?.LastPage is int lastPage && pageNumber >= lastPage)
            {
                break;
            }

            pageNumber = nextPage.Value;
        }

        return new ProfileSearchResponse
        {
            Counts = lastResponse?.Counts,
            Results = allResults.Take(maxResults).ToList(),
            Pagination = lastResponse?.Pagination
        };
    }

    private async Task<ProfileSearchResponse> SearchSinglePageAsync(ProfileSearchRequest request, int pageNumber, int pageSize, bool revealAllData, CancellationToken cancellationToken)
    {
        var payload = new
        {
            parameters = request.BuildParameters(),
            reveal_all_data = revealAllData,
            page_size = pageSize,
            page_number = pageNumber
        };

        using var response = await _httpClient.PostAsJsonAsync(ProfileSearchEndpoint, payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Rhetorik profile search failed with {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<ProfileSearchResponse>(JsonOptions, cancellationToken)
            ?? new ProfileSearchResponse();

        if (result.Errors is { Count: > 0 })
        {
            throw new HttpRequestException(
                $"Rhetorik profile search returned errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
        }

        return result;
    }

    public async Task<IReadOnlyList<AutocompleteSuggestion>> AutocompleteAsync(string field, string inputText, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            field,
            display_count = true,
            page_size = 10,
            page_number = 1,
            parameters = new { input_text = inputText }
        };

        using var response = await _httpClient.PostAsJsonAsync(AutocompleteEndpoint, payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Rhetorik autocomplete failed with {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<AutocompleteResponse>(JsonOptions, cancellationToken);
        return result?.Results ?? [];
    }

    public async Task<IReadOnlyList<Lead>> SearchAndMapToLeadsAsync(ProfileSearchRequest request, CancellationToken cancellationToken = default)
    {
        var result = await SearchProfilesAsync(request, cancellationToken);
        return result.Results
            .Where(r => r.ProfileData is not null)
            .Select(MapToLead)
            .ToList();
    }

    private static Lead MapToLead(RhetorikProfileResult r)
    {
        var p = r.ProfileData!;
        var currentExperience = r.ContactData?.CurrentExperiences?
            .FirstOrDefault(e => e.Current == true) ?? r.ContactData?.CurrentExperiences?.FirstOrDefault();

        var workExperience = r.ResumeData?.Experiences?
            .Select(e => new { company = e.RawCompanyName ?? e.CompanyName, title = e.JobTitle, current = e.Current ?? false, startDate = e.StartDate, endDate = e.EndDate })
            .ToList();

        var education = r.ResumeData?.Educations?
            .Select(e => new { school = e.EducationalEstablishment, degree = e.Diploma, specialization = e.Specialization, startDate = e.StartDate, endDate = e.EndDate })
            .ToList();

        var certifications = r.ResumeData?.Certifications?
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var memberships = r.ResumeData?.Memberships?
            .Select(m => m.Name ?? m.Title)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var publications = r.ResumeData?.Publications?
            .Select(p => p.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var awards = r.ResumeData?.Awards?
            .Select(a => a.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var patents = r.ResumeData?.Patents?
            .Select(p => p.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var languages = p.Languages?.ToList();
        var industries = p.Tags?.Where(t => t.Contains("Industry") || t.Contains("industry")).ToList();

        return new Lead
        {
            ExternalId = Truncate(p.ProfileId ?? string.Empty, 100),
            FirstName = Truncate((p.FirstName ?? string.Empty).Trim(), 100),
            LastName = Truncate((p.LastName ?? string.Empty).Trim(), 100),
            Email = string.Empty,
            Company = TruncateNullable(currentExperience?.RawCompanyName ?? currentExperience?.CompanyName, 200),
            JobTitle = TruncateNullable(currentExperience?.JobTitle ?? p.Headline, 200),
            Location = TruncateNullable(BuildLocation(p.Address), 200),
            LinkedInUrl = RhetorikSocialLinks.ExtractLinkedInUrl(p.SocialLinks),
            Source = Truncate($"Rhetorik:{ProfileSearchEndpoint}", 100),
            Profile = new LeadProfile
            {
                Headline = TruncateNullable(p.Headline, 500),
                Summary = p.Summary,
                SelfReportedSkills = p.Expertises is { Count: > 0 } ? string.Join(", ", p.Expertises) : null,
                WorkExperience = workExperience is { Count: > 0 } ? System.Text.Json.JsonSerializer.Serialize(workExperience) : null,
                Education = education is { Count: > 0 } ? System.Text.Json.JsonSerializer.Serialize(education) : null,
                Certifications = certifications is { Count: > 0 } ? string.Join(", ", certifications) : null,
                Industries = industries is { Count: > 0 } ? string.Join(", ", industries) : null,
                Languages = languages is { Count: > 0 } ? string.Join(", ", languages) : null,
                Memberships = memberships is { Count: > 0 } ? string.Join(", ", memberships) : null,
                Publications = publications is { Count: > 0 } ? string.Join(", ", publications) : null,
                Awards = awards is { Count: > 0 } ? string.Join(", ", awards) : null,
                Patents = patents is { Count: > 0 } ? string.Join(", ", patents) : null
            }
        };
    }

    private static string? BuildLocation(RhetorikAddress? address)
    {
        if (address is null)
        {
            return null;
        }

        var parts = new[] { address.City, address.State, address.Country }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToList();

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? TruncateNullable(string? value, int maxLength)
    {
        return value is null ? null : Truncate(value, maxLength);
    }
}




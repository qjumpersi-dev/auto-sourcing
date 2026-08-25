using System.Net.Http.Json;
using System.Text.Json;
using AutoSourcing.Core.Entities;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Rhetorik;

public class RhetorikClient : IRhetorikClient
{
    private const string ProfileSearchEndpoint = "profile/search";
    private const string AutocompleteEndpoint = "autocomplete";

    private const bool RevealAllData = false;
    private const int MaxPageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public RhetorikClient(HttpClient httpClient, IOptions<RhetorikOptions> options)
    {
        _httpClient = httpClient;
        var opts = options.Value;
        _httpClient.BaseAddress ??= new Uri(opts.BaseUrl);
        _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", opts.ApiKey);
    }

    public async Task<ProfileSearchResponse> SearchProfilesAsync(ProfileSearchRequest request, CancellationToken cancellationToken = default)
    {
        var maxResults = Math.Clamp(request.MaxResults, 1, 1000);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var allResults = new List<RhetorikProfileResult>();
        ProfileSearchResponse? lastResponse = null;

        while (allResults.Count < maxResults)
        {
            var response = await SearchSinglePageAsync(request, pageNumber, pageSize, cancellationToken);
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

    private async Task<ProfileSearchResponse> SearchSinglePageAsync(ProfileSearchRequest request, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var payload = new
        {
            parameters = request.BuildParameters(),
            reveal_all_data = RevealAllData,
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
            .FirstOrDefault(e => e.Current) ?? r.ContactData?.CurrentExperiences?.FirstOrDefault();

        return new Lead
        {
            ExternalId = p.ProfileId,
            FirstName = p.FirstName,
            LastName = p.LastName,
            Email = string.Empty,
            Company = currentExperience?.CompanyName ?? p.Headline,
            JobTitle = currentExperience?.JobTitle ?? p.Headline,
            LinkedInUrl = null,
            Source = $"Rhetorik:{ProfileSearchEndpoint}"
        };
    }
}

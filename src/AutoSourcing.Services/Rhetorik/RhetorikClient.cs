using System.Net.Http.Json;
using System.Text.Json;
using AutoSourcing.Core.Entities;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Rhetorik;

public class RhetorikClient : IRhetorikClient
{
    private const string SearchEndpoint = "contact/search";

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

    public async Task<RhetorikSearchResponse> SearchContactsAsync(RhetorikSearchRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            parameters = request.BuildParameters(),
            reveal_all_data = request.RevealAllData,
            page_size = request.PageSize,
            page_number = request.PageNumber
        };

        using var response = await _httpClient.PostAsJsonAsync(SearchEndpoint, payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Rhetorik contact search failed with {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<RhetorikSearchResponse>(JsonOptions, cancellationToken)
            ?? new RhetorikSearchResponse();

        if (result.Errors is { Count: > 0 })
        {
            throw new HttpRequestException(
                $"Rhetorik contact search returned errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
        }

        return result;
    }

    public async Task<IReadOnlyList<Lead>> SearchAndMapToLeadsAsync(RhetorikSearchRequest request, CancellationToken cancellationToken = default)
    {
        var result = await SearchContactsAsync(request, cancellationToken);
        return result.Results
            .Where(r => r.ContactData is not null)
            .Select(r => MapToLead(r.ContactData!))
            .ToList();
    }

    private static Lead MapToLead(RhetorikContactData c) => new()
    {
        ExternalId = c.ContactId,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Email = c.Emails?.FirstOrDefault()?.Address ?? string.Empty,
        Phone = c.Phones?.FirstOrDefault()?.Number,
        Company = c.CompanyName,
        JobTitle = c.JobTitle,
        LinkedInUrl = c.SocialLinks?
            .FirstOrDefault(s => s.Name.Equals("linkedin", StringComparison.OrdinalIgnoreCase))?.Url,
        Source = $"Rhetorik:{SearchEndpoint}"
    };
}

using System.Net.Http.Json;
using AutoSourcing.Core.Entities;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Rhetorik;

public class RhetorikClient : IRhetorikClient
{
    private readonly HttpClient _httpClient;

    public RhetorikClient(HttpClient httpClient, IOptions<RhetorikOptions> options)
    {
        _httpClient = httpClient;
        var opts = options.Value;
        _httpClient.BaseAddress ??= new Uri(opts.BaseUrl);
        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", opts.ApiKey);
        }
    }

    public async Task<RhetorikSearchResponse> SearchProfilesAsync(RhetorikSearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RhetorikSearchResponse>(cancellationToken)
            ?? new RhetorikSearchResponse();
    }

    public async Task<IReadOnlyList<Lead>> SearchAndMapToLeadsAsync(RhetorikSearchRequest request, CancellationToken cancellationToken = default)
    {
        var result = await SearchProfilesAsync(request, cancellationToken);
        return result.Results.Select(MapToLead).ToList();
    }

    private static Lead MapToLead(RhetorikProfile profile) => new()
    {
        ExternalId = profile.ExternalId,
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        Email = profile.Email ?? string.Empty,
        Phone = profile.Phone,
        Company = profile.Company,
        JobTitle = profile.JobTitle,
        LinkedInUrl = profile.LinkedInUrl,
        Source = "Rhetorik"
    };
}

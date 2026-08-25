using AutoSourcing.Core.Entities;

namespace AutoSourcing.Services.Rhetorik;

public interface IRhetorikClient
{
    Task<ProfileSearchResponse> SearchProfilesAsync(ProfileSearchRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AutocompleteSuggestion>> AutocompleteAsync(string field, string inputText, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lead>> SearchAndMapToLeadsAsync(ProfileSearchRequest request, CancellationToken cancellationToken = default);
}

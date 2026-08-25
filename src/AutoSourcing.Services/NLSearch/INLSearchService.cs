using AutoSourcing.Services.Rhetorik;

namespace AutoSourcing.Services.NLSearch;

public interface INLSearchService
{
    Task<ProfileSearchRequest> GenerateSearchSpecAsync(string freeText, CancellationToken cancellationToken = default);
}

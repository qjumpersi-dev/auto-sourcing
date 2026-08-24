using AutoSourcing.Core.Entities;

namespace AutoSourcing.Services.Rhetorik;

public interface IRhetorikClient
{
    Task<RhetorikSearchResponse> SearchContactsAsync(RhetorikSearchRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lead>> SearchAndMapToLeadsAsync(RhetorikSearchRequest request, CancellationToken cancellationToken = default);
}

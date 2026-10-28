using Microsoft.Extensions.Caching.Memory;

namespace AutoSourcing.Services.Rhetorik;

public interface ILocationBreakdownService
{
    Task<LocationBreakdownResponse> GetBreakdownAsync(LocationBreakdownRequest request, CancellationToken cancellationToken = default);
}

public class LocationBreakdownRequest
{
    public ProfileSearchRequest Search { get; set; } = new();
    public string Level { get; set; } = string.Empty;
    public List<string>? Regions { get; set; }
    public List<LocationCount>? Sample { get; set; }
    public int? TotalMatched { get; set; }
    public int? Returned { get; set; }
}

public class LocationCount
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class LocationBreakdownResponse
{
    public string Level { get; set; } = string.Empty;
    public List<LocationCount> Regions { get; set; } = [];
    public List<string> Unavailable { get; set; } = [];
    public bool Complete { get; set; }
}

public enum LocationBreakdownLevel
{
    Country,
    State,
    City
}

public static class LocationBreakdownLevels
{
    public const int MaxRegions = 150;

    public static bool TryParse(string? value, out LocationBreakdownLevel level)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "country":
                level = LocationBreakdownLevel.Country;
                return true;
            case "state":
                level = LocationBreakdownLevel.State;
                return true;
            case "city":
                level = LocationBreakdownLevel.City;
                return true;
            default:
                level = default;
                return false;
        }
    }

    public static string ToName(LocationBreakdownLevel level)
    {
        return level switch
        {
            LocationBreakdownLevel.Country => "country",
            LocationBreakdownLevel.State => "state",
            LocationBreakdownLevel.City => "city",
            _ => string.Empty,
        };
    }
}

public class LocationBreakdownService : ILocationBreakdownService
{
    private const int MaxAttempts = 3;
    private const int MaxConcurrency = 15;
    private const int RetryDelayMilliseconds = 500;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static readonly SemaphoreSlim Throttle = new(MaxConcurrency, MaxConcurrency);

    private readonly IRhetorikClient _rhetorikClient;
    private readonly IMemoryCache _cache;

    public LocationBreakdownService(IRhetorikClient rhetorikClient, IMemoryCache cache)
    {
        _rhetorikClient = rhetorikClient;
        _cache = cache;
    }

    public async Task<LocationBreakdownResponse> GetBreakdownAsync(LocationBreakdownRequest request, CancellationToken cancellationToken = default)
    {
        if (!LocationBreakdownLevels.TryParse(request.Level, out var level))
        {
            throw new ArgumentException($"Unknown breakdown level '{request.Level}'.", nameof(request));
        }

        var cacheKey = BuildCacheKey(request.Search, level, request.Regions);
        if (_cache.TryGetValue(cacheKey, out LocationBreakdownResponse? cached) && cached is not null)
        {
            return cached;
        }

        var sample = CollectSample(request.Sample);

        if (request.TotalMatched is int totalMatched && request.Returned is int returned && totalMatched <= returned)
        {
            var aggregate = new LocationBreakdownResponse
            {
                Level = LocationBreakdownLevels.ToName(level),
                Regions = sample.Values
                    .OrderByDescending(v => v.Count)
                    .Select(v => new LocationCount { Name = v.Name, Count = v.Count })
                    .ToList(),
                Complete = true
            };
            _cache.Set(cacheKey, aggregate, CacheDuration);
            return aggregate;
        }

        var (regions, truncated) = BuildRegionList(request.Regions, sample);
        var response = await ProbeRegionsAsync(request.Search, level, regions, sample, truncated, cancellationToken);
        _cache.Set(cacheKey, response, CacheDuration);
        return response;
    }

    private static Dictionary<string, LocationCount> CollectSample(List<LocationCount>? sample)
    {
        var map = new Dictionary<string, LocationCount>(StringComparer.OrdinalIgnoreCase);
        if (sample is null)
        {
            return map;
        }

        foreach (var entry in sample)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var name = entry.Name.Trim();
            if (map.TryGetValue(name, out var existing))
            {
                existing.Count = Math.Max(existing.Count, entry.Count);
                continue;
            }

            map[name] = new LocationCount { Name = name, Count = entry.Count };
        }

        return map;
    }

    private static (List<string> Regions, bool Truncated) BuildRegionList(List<string>? canonical, Dictionary<string, LocationCount> sample)
    {
        var regions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var trimmed = name.Trim();
            if (!seen.Add(trimmed) || regions.Count >= LocationBreakdownLevels.MaxRegions)
            {
                return;
            }

            regions.Add(trimmed);
        }

        foreach (var region in canonical ?? [])
        {
            Add(region);
        }

        foreach (var region in sample.Values)
        {
            Add(region.Name);
        }

        return (regions, seen.Count > regions.Count);
    }

    private async Task<LocationBreakdownResponse> ProbeRegionsAsync(
        ProfileSearchRequest search,
        LocationBreakdownLevel level,
        List<string> regions,
        Dictionary<string, LocationCount> sample,
        bool truncated,
        CancellationToken cancellationToken)
    {
        var probes = regions
            .Select(region => ProbeWithRetryAsync(search, level, region, cancellationToken))
            .ToList();

        var counts = await Task.WhenAll(probes);

        var response = new LocationBreakdownResponse
        {
            Level = LocationBreakdownLevels.ToName(level),
            Unavailable = []
        };

        var regionCounts = new List<LocationCount>();
        for (var i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            var sampleCount = sample.TryGetValue(region, out var existing) ? existing.Count : 0;

            if (counts[i] is int probed)
            {
                var count = probed == 0 && sampleCount > 0 ? sampleCount : probed;
                regionCounts.Add(new LocationCount { Name = region, Count = count });
            }
            else
            {
                response.Unavailable.Add(region);
                if (sampleCount > 0)
                {
                    regionCounts.Add(new LocationCount { Name = region, Count = sampleCount });
                }
            }
        }

        response.Regions = regionCounts.OrderByDescending(r => r.Count).ToList();
        response.Complete = !truncated && response.Unavailable.Count == 0;
        return response;
    }

    private async Task<int?> ProbeWithRetryAsync(ProfileSearchRequest search, LocationBreakdownLevel level, string region, CancellationToken cancellationToken)
    {
        var probe = BuildProbe(search, level, region);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await Throttle.WaitAsync(cancellationToken);
                try
                {
                    return await _rhetorikClient.CountProfilesAsync(probe, cancellationToken);
                }
                finally
                {
                    Throttle.Release();
                }
            }
            catch (HttpRequestException) when (attempt < MaxAttempts)
            {
                await Task.Delay(RetryDelayMilliseconds * attempt, cancellationToken);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        return null;
    }

    private static ProfileSearchRequest BuildProbe(ProfileSearchRequest search, LocationBreakdownLevel level, string region)
    {
        return new ProfileSearchRequest
        {
            ProfileIds = search.ProfileIds,
            Keywords = search.Keywords,
            JobTitles = search.JobTitles,
            JobTitleScope = search.JobTitleScope,
            Companies = search.Companies,
            CompanyScope = search.CompanyScope,
            Expertises = search.Expertises,
            ExpertiseMode = search.ExpertiseMode,
            Countries = level == LocationBreakdownLevel.Country ? [region] : search.Countries,
            States = level == LocationBreakdownLevel.State ? [region] : search.States,
            Cities = level == LocationBreakdownLevel.City ? [region] : search.Cities,
            JobTitleSuggestions = search.JobTitleSuggestions,
            PageNumber = 1,
            PageSize = 1,
            MaxResults = 1
        };
    }

    private static string BuildCacheKey(ProfileSearchRequest search, LocationBreakdownLevel level, List<string>? regions)
    {
        static string Pack(IEnumerable<string>? values)
        {
            return values is null
                ? string.Empty
                : string.Join("|", values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim().ToLowerInvariant()));
        }

        static string Lower(string? value)
        {
            return value?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        return string.Join(':',
            "location-breakdown",
            LocationBreakdownLevels.ToName(level),
            Pack(search.ProfileIds),
            Pack(search.Keywords),
            Pack(search.JobTitles),
            Lower(search.JobTitleScope),
            Pack(search.Companies),
            Lower(search.CompanyScope),
            Pack(search.Expertises),
            Lower(search.ExpertiseMode),
            Pack(search.Countries),
            Pack(search.States),
            Pack(search.Cities),
            Pack(regions));
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using AutoSourcing.Services.Rhetorik;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AutoSourcing.Tests;

public class LocationBreakdownServiceTests
{
    private static (LocationBreakdownService Service, CountingHandler Handler) CreateService(Func<int, HttpResponseMessage> responder)
    {
        var handler = new CountingHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.rhetorik360.io/") };
        var options = Substitute.For<IOptions<RhetorikOptions>>();
        options.Value.Returns(new RhetorikOptions { BaseUrl = "https://api.rhetorik360.io/", ApiKey = "test-key" });
        var rhetorikClient = new RhetorikClient(httpClient, options);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new LocationBreakdownService(rhetorikClient, cache), handler);
    }

    private static HttpResponseMessage Json(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task GetBreakdownAsync_WhenAllMatchesReturned_UsesSampleOnly()
    {
        var (sut, handler) = CreateService(_ => Json("""{ "results": [], "counts": { "profiles_total_results": 10 } }"""));

        var response = await sut.GetBreakdownAsync(new LocationBreakdownRequest
        {
            Search = new ProfileSearchRequest { Countries = ["United States"] },
            Level = "state",
            Regions = ["Texas", "California"],
            Sample =
            [
                new LocationCount { Name = "Texas", Count = 7 },
                new LocationCount { Name = "California", Count = 3 }
            ],
            TotalMatched = 10,
            Returned = 10
        });

        Assert.Equal(0, handler.RequestCount);
        Assert.True(response.Complete);
        Assert.Empty(response.Unavailable);
        Assert.Equal(7, response.Regions.Single(r => r.Name == "Texas").Count);
        Assert.Equal(3, response.Regions.Single(r => r.Name == "California").Count);
    }

    [Fact]
    public async Task GetBreakdownAsync_WhenMoreMatchesThanReturned_ProbesEachRegionWithCriteria()
    {
        var (sut, handler) = CreateService(_ => Json("""{ "results": [], "counts": { "profiles_total_results": 2500 } }"""));

        var response = await sut.GetBreakdownAsync(new LocationBreakdownRequest
        {
            Search = new ProfileSearchRequest { Countries = ["United States"], JobTitles = ["Developer"] },
            Level = "state",
            Regions = ["Texas", "California"],
            Sample = [new LocationCount { Name = "Texas", Count = 5 }],
            TotalMatched = 2500,
            Returned = 500
        });

        Assert.Equal(2, handler.RequestCount);
        Assert.True(response.Complete);
        Assert.Empty(response.Unavailable);
        Assert.Equal(2500, response.Regions.Single(r => r.Name == "Texas").Count);
        Assert.Equal(2500, response.Regions.Single(r => r.Name == "California").Count);

        var probedStates = handler.RequestBodies
            .Select(b => GetStringValues(b.GetProperty("parameters").GetProperty("states")).Single())
            .OrderBy(s => s)
            .ToList();
        Assert.Equal(["California", "Texas"], probedStates);

        foreach (var body in handler.RequestBodies)
        {
            Assert.Equal(1, body.GetProperty("page_size").GetInt32());
            Assert.Equal(1, body.GetProperty("page_number").GetInt32());
            Assert.False(body.GetProperty("reveal_all_data").GetBoolean());

            var parameters = body.GetProperty("parameters");
            Assert.Equal(["United States"], GetStringValues(parameters.GetProperty("countries")));
            Assert.Equal(["Developer"], GetStringValues(parameters.GetProperty("job_titles")));
        }
    }

    [Fact]
    public async Task GetBreakdownAsync_CityLevel_ProbesCitiesWithinSelectedStates()
    {
        var (sut, handler) = CreateService(_ => Json("""{ "results": [], "counts": { "profiles_total_results": 700 } }"""));

        var response = await sut.GetBreakdownAsync(new LocationBreakdownRequest
        {
            Search = new ProfileSearchRequest { Countries = ["United States"], States = ["Texas", "California"] },
            Level = "city",
            Regions = ["Houston", "Austin"],
            TotalMatched = 700,
            Returned = 500
        });

        Assert.Equal(2, handler.RequestCount);
        Assert.True(response.Complete);

        var probedCities = handler.RequestBodies
            .Select(b => GetStringValues(b.GetProperty("parameters").GetProperty("cities")).Single())
            .OrderBy(c => c)
            .ToList();
        Assert.Equal(["Austin", "Houston"], probedCities);

        foreach (var body in handler.RequestBodies)
        {
            var parameters = body.GetProperty("parameters");
            var states = GetStringValues(parameters.GetProperty("states")).OrderBy(s => s).ToList();
            Assert.Equal(["California", "Texas"], states);
        }
    }

    [Fact]
    public async Task GetBreakdownAsync_CountryLevel_ProbesCountryFacet()
    {
        var (sut, handler) = CreateService(_ => Json("""{ "results": [], "counts": { "profiles_total_results": 1200 } }"""));

        var response = await sut.GetBreakdownAsync(new LocationBreakdownRequest
        {
            Search = new ProfileSearchRequest { Keywords = ["recruiter"] },
            Level = "country",
            Regions = ["New Zealand"],
            Sample = [new LocationCount { Name = "United States", Count = 200 }],
            TotalMatched = 1200,
            Returned = 500
        });

        Assert.Equal(2, handler.RequestCount);

        var probedCountries = handler.RequestBodies
            .Select(b => GetStringValues(b.GetProperty("parameters").GetProperty("countries")).Single())
            .OrderBy(c => c)
            .ToList();
        Assert.Equal(["New Zealand", "United States"], probedCountries);
        Assert.Equal(2400, response.Regions.Sum(r => r.Count));
    }

    [Fact]
    public async Task GetBreakdownAsync_WhenProbeCountIsZeroButSampleHasMatches_KeepsSampleCount()
    {
        var (sut, _) = CreateService(_ => Json("""{ "results": [], "counts": { "profiles_total_results": 0 } }"""));

        var response = await sut.GetBreakdownAsync(new LocationBreakdownRequest
        {
            Search = new ProfileSearchRequest { Countries = ["Germany"] },
            Level = "state",
            Regions = ["Bayern"],
            Sample = [new LocationCount { Name = "Bayern", Count = 9 }],
            TotalMatched = 5000,
            Returned = 500
        });

        Assert.Equal(9, response.Regions.Single().Count);
    }

    [Fact]
    public async Task GetBreakdownAsync_RetriesTransientFailures()
    {
        var calls = 0;
        var (sut, handler) = CreateService(_ =>
        {
            calls++;
            return calls == 1
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent("""{ "error": "rate limited" }""", Encoding.UTF8, "application/json")
                }
                : Json("""{ "results": [], "counts": { "profiles_total_results": 42 } }""");
        });

        var response = await sut.GetBreakdownAsync(new LocationBreakdownRequest
        {
            Search = new ProfileSearchRequest { Countries = ["United States"] },
            Level = "state",
            Regions = ["Texas"],
            TotalMatched = 900,
            Returned = 500
        });

        Assert.Equal(2, handler.RequestCount);
        Assert.True(response.Complete);
        Assert.Equal(42, response.Regions.Single().Count);
    }

    [Fact]
    public async Task GetBreakdownAsync_MarksRegionsUnavailable_AfterFailedProbes()
    {
        var (sut, _) = CreateService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        var response = await sut.GetBreakdownAsync(new LocationBreakdownRequest
        {
            Search = new ProfileSearchRequest { Countries = ["United States"] },
            Level = "state",
            Regions = ["Texas", "California"],
            Sample = [new LocationCount { Name = "Texas", Count = 5 }],
            TotalMatched = 900,
            Returned = 500
        });

        Assert.False(response.Complete);
        Assert.Contains("Texas", response.Unavailable);
        Assert.Contains("California", response.Unavailable);
        Assert.Equal(5, response.Regions.Single().Count);
    }

    [Fact]
    public async Task GetBreakdownAsync_CachesProbeResults()
    {
        var (sut, handler) = CreateService(_ => Json("""{ "results": [], "counts": { "profiles_total_results": 2500 } }"""));

        var request = new LocationBreakdownRequest
        {
            Search = new ProfileSearchRequest { Countries = ["United States"] },
            Level = "state",
            Regions = ["Texas", "California"],
            TotalMatched = 900,
            Returned = 500
        };

        var first = await sut.GetBreakdownAsync(request);
        var second = await sut.GetBreakdownAsync(request);

        Assert.Equal(2, handler.RequestCount);
        Assert.Equal(first.Regions.Count, second.Regions.Count);
        Assert.Equal(2500, second.Regions.Single(r => r.Name == "Texas").Count);
    }

    [Fact]
    public async Task GetBreakdownAsync_ThrowsOnUnknownLevel()
    {
        var (sut, _) = CreateService(_ => Json("""{ "results": [], "counts": {} }"""));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetBreakdownAsync(new LocationBreakdownRequest { Level = "county" }));
    }

    private static List<string> GetStringValues(JsonElement conditionElement)
    {
        return conditionElement[0].GetProperty("value")
            .EnumerateArray()
            .Select(v => v.GetString()!)
            .ToList();
    }

    private sealed class CountingHandler(Func<int, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly object _gate = new();

        public int RequestCount { get; private set; }

        public List<JsonElement> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            int number;

            lock (_gate)
            {
                RequestCount++;
                number = RequestCount;
                RequestBodies.Add(default);
            }

            if (request.Content is not null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken);
                lock (_gate)
                {
                    RequestBodies[number - 1] = JsonDocument.Parse(content).RootElement.Clone();
                }
            }

            response = responder(number);
            return response;
        }
    }
}

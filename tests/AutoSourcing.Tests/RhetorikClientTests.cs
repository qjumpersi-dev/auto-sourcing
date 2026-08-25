using System.Net;
using System.Text.Json;
using AutoSourcing.Services.Rhetorik;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AutoSourcing.Tests;

public class RhetorikClientTests
{
    private static (RhetorikClient Client, FakeHttpMessageHandler Handler) CreateClient(string responseJson)
    {
        var handler = new FakeHttpMessageHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.rhetorik360.io/") };
        var options = Substitute.For<IOptions<RhetorikOptions>>();
        options.Value.Returns(new RhetorikOptions { BaseUrl = "https://api.rhetorik360.io/", ApiKey = "test-key" });
        return (new RhetorikClient(httpClient, options), handler);
    }

    [Fact]
    public async Task SearchProfilesAsync_ParsesProfileResponse()
    {
        const string json = """
            {
              "counts": { "profiles_total_results": 42, "profiles_total_returned": 1 },
              "results": [
                {
                  "position": 1,
                  "profile_data": {
                    "profile_id": "prof-abc-123",
                    "profile_first_name": "Jane",
                    "profile_last_name": "Doe",
                    "profile_headline": "Senior Developer at Acme",
                    "profile_expertises": ["C#", ".NET"],
                    "profile_tags": ["Profile Has Email"],
                    "profile_address": { "country": "New Zealand", "state": "Auckland", "city": "Auckland" }
                  },
                  "contact_data": {
                    "contact_current_experiences": [
                      { "company_name": "Acme Corp", "job_title": "Senior Developer", "current": true }
                    ]
                  }
                }
              ],
              "pagination": { "current": 1, "last_page": 9, "next_page": null }
            }
            """;

        var (sut, _) = CreateClient(json);
        var result = await sut.SearchProfilesAsync(new ProfileSearchRequest());

        Assert.Equal(42, result.Counts?.ProfilesTotalResults);
        var profile = Assert.Single(result.Results);
        Assert.Equal("Jane", profile.ProfileData!.FirstName);
        Assert.Equal("New Zealand", profile.ProfileData.Address?.Country);
    }

    [Fact]
    public async Task SearchAndMapToLeadsAsync_MapsCurrentExperience()
    {
        const string json = """
            {
              "results": [
                {
                  "position": 1,
                  "profile_data": {
                    "profile_id": "prof-xyz-9",
                    "profile_first_name": "John",
                    "profile_last_name": "Smith"
                  },
                  "contact_data": {
                    "contact_current_experiences": [
                      { "company_name": "Beta Ltd", "job_title": "Past Job", "current": false },
                      { "company_name": "Acme Corp", "job_title": "Developer", "current": true }
                    ]
                  }
                }
              ]
            }
            """;

        var (sut, _) = CreateClient(json);
        var leads = await sut.SearchAndMapToLeadsAsync(new ProfileSearchRequest());

        var lead = Assert.Single(leads);
        Assert.Equal("John", lead.FirstName);
        Assert.Equal("Acme Corp", lead.Company);
        Assert.Equal("Developer", lead.JobTitle);
        Assert.StartsWith("Rhetorik:", lead.Source);
        Assert.Equal("prof-xyz-9", lead.ExternalId);
    }

    [Fact]
    public async Task SearchProfilesAsync_BuildsPayload_WithScopesModesAndEmailTag()
    {
        const string json = """{ "results": [], "counts": {} }""";
        var (sut, handler) = CreateClient(json);

        await sut.SearchProfilesAsync(new ProfileSearchRequest
        {
            Keywords = ["recruiter"],
            JobTitles = ["Head of Talent"],
            JobTitleScope = "current",
            Companies = ["Acme Corp"],
            CompanyScope = "past",
            Expertises = ["C#", "SQL"],
            ExpertiseMode = ProfileSearchRequest.MustHaveAll,
            Countries = ["New Zealand"],
            States = ["Auckland"],
            Cities = ["Wellington"],
            PageNumber = 2,
            PageSize = 50
        });

        var body = handler.LastRequestBody;

        Assert.False(body.GetProperty("reveal_all_data").GetBoolean());
        Assert.Equal(50, body.GetProperty("page_size").GetInt32());
        Assert.Equal(2, body.GetProperty("page_number").GetInt32());

        var p = body.GetProperty("parameters");

        Assert.Equal(
            new List<string> { "recruiter" },
            GetStringValues(p.GetProperty("keywords")));
        Assert.Equal(
            new List<string> { "Head of Talent" },
            GetStringValues(p.GetProperty("current_job_titles")));
        Assert.Equal(
            new List<string> { "Acme Corp" },
            GetStringValues(p.GetProperty("previous_company_names")));

        var expertises = p.GetProperty("expertises");
        Assert.Equal(2, expertises.GetArrayLength());
        Assert.Equal("is", expertises[0].GetProperty("operator").GetString());
        Assert.Equal(new List<string> { "C#" }, ValueList(expertises[0]));
        Assert.Equal("is", expertises[1].GetProperty("operator").GetString());
        Assert.Equal(new List<string> { "SQL" }, ValueList(expertises[1]));

        Assert.Equal(
            new List<string> { "New Zealand" },
            GetStringValues(p.GetProperty("countries")));
        Assert.Equal(
            new List<string> { "Auckland" },
            GetStringValues(p.GetProperty("states")));
        Assert.Equal(
            new List<string> { "Wellington" },
            GetStringValues(p.GetProperty("cities")));

        var tags = p.GetProperty("profile_tags");
        Assert.Equal("is one of", tags[0].GetProperty("operator").GetString());
        Assert.Equal(
            new List<string> { "Profile Has Email" },
            ValueList(tags[0]));
    }

    [Fact]
    public async Task SearchProfilesAsync_MustNotHaveAny_UsesIsNotOneOf()
    {
        const string json = """{ "results": [], "counts": {} }""";
        var (sut, handler) = CreateClient(json);

        await sut.SearchProfilesAsync(new ProfileSearchRequest
        {
            Expertises = ["Java"],
            ExpertiseMode = ProfileSearchRequest.MustNotHaveAny
        });

        var expertises = handler.LastRequestBody.GetProperty("parameters").GetProperty("expertises");
        Assert.Equal(1, expertises.GetArrayLength());
        Assert.Equal("is not one of", expertises[0].GetProperty("operator").GetString());
    }

    [Fact]
    public async Task SearchProfilesAsync_Paginates_UpToMaxResults()
    {
        var page = 0;
        var handler = new PagingFakeHandler(() =>
        {
            page++;
            return "{ \"counts\": { \"profiles_total_results\": 250 }, \"results\": [ { \"position\": 1, \"profile_data\": { \"profile_id\": \"p-" + page + "-a\", \"profile_first_name\": \"A\", \"profile_last_name\": \"One\" } }, { \"position\": 2, \"profile_data\": { \"profile_id\": \"p-" + page + "-b\", \"profile_first_name\": \"B\", \"profile_last_name\": \"Two\" } } ], \"pagination\": { \"current\": " + page + ", \"last_page\": 5, \"next_page\": " + (page + 1) + " } }";
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.rhetorik360.io/") };
        var options = Substitute.For<IOptions<RhetorikOptions>>();
        options.Value.Returns(new RhetorikOptions());

        var sut = new RhetorikClient(httpClient, options);
        var result = await sut.SearchProfilesAsync(new ProfileSearchRequest { MaxResults = 6, PageSize = 100 });

        Assert.Equal(3, handler.RequestCount);
        Assert.Equal(6, result.Results.Count);
        Assert.Equal("p-1-a", result.Results[0].ProfileData!.ProfileId);
        Assert.Equal("p-3-b", result.Results[5].ProfileData!.ProfileId);
    }

    [Fact]
    public async Task SearchProfilesAsync_ThrowsWithErrorBody_OnNonSuccess()
    {
        var handler = new FakeHttpMessageHandler("""{ "errors": [{ "message": "Invalid API key" }] }""", HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.rhetorik360.io/") };
        var options = Substitute.For<IOptions<RhetorikOptions>>();
        options.Value.Returns(new RhetorikOptions());

        var sut = new RhetorikClient(httpClient, options);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.SearchProfilesAsync(new ProfileSearchRequest()));
    }

    [Fact]
    public async Task AutocompleteAsync_BuildsPayload_AndParsesSuggestions()
    {
        const string json = """
            {
              "results": [
                { "position": 1, "count": 500, "content": "New Zealand", "group": "Oceania" },
                { "position": 2, "count": 10, "content": "Netherlands", "group": "Europe" }
              ]
            }
            """;
        var (sut, handler) = CreateClient(json);

        var suggestions = await sut.AutocompleteAsync("countries", "ne");

        var payload = handler.LastRequestBody;
        Assert.Equal("countries", payload.GetProperty("field").GetString());
        Assert.True(payload.GetProperty("display_count").GetBoolean());
        Assert.Equal("ne", payload.GetProperty("parameters").GetProperty("input_text").GetString());

        Assert.Equal(2, suggestions.Count);
        Assert.Equal("New Zealand", suggestions[0].Content);
        Assert.Equal(500, suggestions[0].Count);
    }


    private static List<string> ValueList(JsonElement conditionObject)
    {
        return conditionObject.GetProperty("value")
            .EnumerateArray()
            .Select(v => v.GetString()!)
            .ToList();
    }
    private static List<string> GetStringValues(JsonElement conditionElement)
    {
        return conditionElement[0].GetProperty("value")
            .EnumerateArray()
            .Select(v => v.GetString()!)
            .ToList();
    }

    private sealed class PagingFakeHandler(Func<string> bodyFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var content = await request.Content!.ReadAsStringAsync(cancellationToken);
            var pageNumber = JsonDocument.Parse(content).RootElement.GetProperty("page_number").GetInt32();
            if (pageNumber >= 4)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{ "results": [], "counts": {}, "pagination": { "current": 4, "last_page": 5, "next_page": null } }""", System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(bodyFactory(), System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
    private sealed class FakeHttpMessageHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public JsonElement LastRequestBody { get; private set; } = default;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken);
                LastRequestBody = JsonDocument.Parse(content).RootElement.Clone();
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}


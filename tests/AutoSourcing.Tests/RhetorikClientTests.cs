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
    public async Task SearchContactsAsync_ParsesCompleteContactResponse()
    {
        const string json = """
            {
              "counts": { "contacts_total_results": 1, "contacts_total_returned": 1 },
              "results": [
                {
                  "position": 1,
                  "contact_data": {
                    "contact_id": "cont-shli-93e0cd9ef1cef51120c8d3f040db7a80",
                    "contact_first_name": "Jane",
                    "contact_last_name": "Doe",
                    "contact_emails": [
                      { "email": "jane.doe@example.com", "reason": "", "type": "professional" }
                    ],
                    "company_name": "Acme Corp",
                    "job_title": "Head of Talent",
                    "contact_country": "New Zealand"
                  }
                }
              ],
              "pagination": { "current": 1, "last_page": 1, "next_page": null }
            }
            """;

        var (sut, _) = CreateClient(json);
        var result = await sut.SearchContactsAsync(new RhetorikSearchRequest { JobTitles = ["Talent"] });

        Assert.Equal(1, result.Counts?.ContactsTotalResults);
        var contact = Assert.Single(result.Results);
        Assert.Equal("Jane", contact.ContactData!.FirstName);
        Assert.Single(contact.ContactData.Emails!);
    }

    [Fact]
    public async Task SearchAndMapToLeadsAsync_MapsContactDataToLead()
    {
        const string json = """
            {
              "results": [
                {
                  "position": 1,
                  "contact_data": {
                    "contact_id": "RH-002",
                    "contact_first_name": "John",
                    "contact_last_name": "Smith",
                    "contact_emails": [{ "email": "john.smith@example.com", "type": "professional" }],
                    "contact_phones": [],
                    "company_name": "Acme Corp",
                    "job_title": "Recruitment Lead",
                    "contact_social_links": [
                      { "name": "linkedin", "url": "https://www.linkedin.com/in/johnsmith", "status": "valid" },
                      { "name": "twitter", "url": "https://twitter.com/johnsmith", "status": "valid" }
                    ]
                  }
                }
              ]
            }
            """;

        var (sut, _) = CreateClient(json);
        var leads = await sut.SearchAndMapToLeadsAsync(new RhetorikSearchRequest());

        var lead = Assert.Single(leads);
        Assert.Equal("John", lead.FirstName);
        Assert.Equal("Smith", lead.LastName);
        Assert.Equal("john.smith@example.com", lead.Email);
        Assert.Equal("Acme Corp", lead.Company);
        Assert.Equal("Recruitment Lead", lead.JobTitle);
        Assert.Equal("https://www.linkedin.com/in/johnsmith", lead.LinkedInUrl);
        Assert.StartsWith("Rhetorik:", lead.Source);
        Assert.Equal("RH-002", lead.ExternalId);
    }

    [Fact]
    public async Task SearchContactsAsync_BuildsParametersPayload()
    {
        const string json = """{ "results": [], "counts": {} }""";
        var (sut, handler) = CreateClient(json);

        await sut.SearchContactsAsync(new RhetorikSearchRequest
        {
            Keywords = ["recruiter"],
            JobTitles = ["Head of Talent"],
            Companies = ["Acme Corp"],
            Countries = ["New Zealand"],
            RevealAllData = true,
            PageNumber = 2,
            PageSize = 50
        });

        var body = handler.LastRequestBody;

        Assert.True(body.TryGetProperty("reveal_all_data", out var reveal));
        Assert.True(reveal.GetBoolean());
        Assert.Equal(50, body.GetProperty("page_size").GetInt32());
        Assert.Equal(2, body.GetProperty("page_number").GetInt32());

        var parameters = body.GetProperty("parameters");
        var keywords = GetStringValues(parameters.GetProperty("keywords"));
        var jobTitles = GetStringValues(parameters.GetProperty("job_title"));
        var companies = GetStringValues(parameters.GetProperty("company_name"));
        var countries = GetStringValues(parameters.GetProperty("country"));

        Assert.Equal(["recruiter"], keywords);
        Assert.Equal(["Head of Talent"], jobTitles);
        Assert.Equal(["Acme Corp"], companies);
        Assert.Equal(["New Zealand"], countries);

        Assert.Equal("includes", parameters.GetProperty("keywords")[0].GetProperty("operator").GetString());
    }

    [Fact]
    public async Task SearchContactsAsync_ThrowsWithErrorBody_OnNonSuccess()
    {
        var handler = new FakeHttpMessageHandler("""{ "errors": [{ "message": "Invalid API key" }] }""", HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.rhetorik360.io/") };
        var options = Substitute.For<IOptions<RhetorikOptions>>();
        options.Value.Returns(new RhetorikOptions());

        var sut = new RhetorikClient(httpClient, options);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.SearchContactsAsync(new RhetorikSearchRequest()));
    }

    private static List<string> GetStringValues(JsonElement parameterElement)
    {
        return parameterElement[0].GetProperty("value")
            .EnumerateArray()
            .Select(v => v.GetString()!)
            .ToList();
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

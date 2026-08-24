using System.Net;
using AutoSourcing.Services.Rhetorik;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AutoSourcing.Tests;

public class RhetorikClientTests
{
    [Fact]
    public async Task SearchProfilesAsync_ParsesResponse()
    {
        const string json = """
            {
              "totalResults": 1,
              "results": [
                {
                  "externalId": "RH-001",
                  "firstName": "Jane",
                  "lastName": "Doe",
                  "email": "jane.doe@example.com",
                  "company": "Acme Corp",
                  "jobTitle": "Head of Talent"
                }
              ]
            }
            """;

        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") };
        var options = Substitute.For<IOptions<RhetorikOptions>>();
        options.Value.Returns(new RhetorikOptions { BaseUrl = "https://api.example.com/", ApiKey = "test-key" });

        var sut = new RhetorikClient(httpClient, options);
        var result = await sut.SearchProfilesAsync(new RhetorikSearchRequest { Keywords = "talent" });

        Assert.Equal(1, result.TotalResults);
        var profile = Assert.Single(result.Results);
        Assert.Equal("Jane", profile.FirstName);
        Assert.Equal("jane.doe@example.com", profile.Email);
    }

    [Fact]
    public async Task SearchAndMapToLeadsAsync_MapsProfileToLead_WithRhetorikSource()
    {
        const string json = """
            {
              "totalResults": 1,
              "results": [
                {
                  "externalId": "RH-002",
                  "firstName": "John",
                  "lastName": "Smith",
                  "email": "john.smith@example.com"
                }
              ]
            }
            """;

        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") };
        var options = Substitute.For<IOptions<RhetorikOptions>>();
        options.Value.Returns(new RhetorikOptions());

        var sut = new RhetorikClient(httpClient, options);
        var leads = await sut.SearchAndMapToLeadsAsync(new RhetorikSearchRequest());

        var lead = Assert.Single(leads);
        Assert.Equal("John", lead.FirstName);
        Assert.Equal("Rhetorik", lead.Source);
        Assert.Equal("RH-002", lead.ExternalId);
    }

    private sealed class FakeHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}

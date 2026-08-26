using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutoSourcing.Services.Rhetorik;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.NLSearch;

public class NLSearchService : INLSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string Prompt =
        """
        Convert the user's recruitment sourcing request into a JSON search specification.
        Return ONLY JSON with this exact shape (omit empty arrays, use [] defaults):
        {
          "keywords": [], "jobTitles": [], "jobTitleScope": "any|current|past",
          "companies": [], "companyScope": "any|current|past",
          "expertises": [], "expertiseMode": "must_have_any|must_have_all|must_not_have_any|must_not_have_all",
          "countries": [], "states": [], "cities": []
        }
        Rules:
        - jobTitleScope: "current" when they want people in the role now; "past" for experience; default "any".
        - companyScope: "current" for employers now; "past" for alumni; default "any".
        - expertiseMode: "must_have_any" unless the request demands all skills ("and", "all of") or excludes skills ("not", "without").
        - Use full country names (e.g. "New Zealand"). Skills go in expertises.
        """;

    private readonly HttpClient _httpClient;
    private readonly NLSearchOptions _options;
    private readonly ILogger<NLSearchService> _logger;

    public NLSearchService(HttpClient httpClient, IOptions<NLSearchOptions> options, ILogger<NLSearchService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProfileSearchRequest> GenerateSearchSpecAsync(string freeText, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.OpenAIApiKey))
        {
            try
            {
                return await GenerateWithOpenAIAsync(freeText, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI spec generation failed, falling back to heuristics.");
            }
        }

        return HeuristicParse(freeText);
    }

    private async Task<ProfileSearchRequest> GenerateWithOpenAIAsync(string freeText, CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAIApiKey);
        requestMessage.Content = JsonContent.Create(new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = Prompt },
                new { role = "user", content = freeText }
            },
            response_format = new { type = "json_object" },
            temperature = 0
        });

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        var content = completion.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";

        var parsed = JsonSerializer.Deserialize<ProfileSearchRequest>(content, JsonOptions) ?? new ProfileSearchRequest();
        Normalize(parsed);
        return parsed;
    }

    private static ProfileSearchRequest HeuristicParse(string text)
    {
        var request = new ProfileSearchRequest();
        if (string.IsNullOrWhiteSpace(text))
        {
            return request;
        }

        var locationMarker = text.IndexOf(" in ", StringComparison.OrdinalIgnoreCase);
        if (locationMarker >= 0)
        {
            var locationPart = text[(locationMarker + 4)..].Trim().TrimEnd('.');
            var segments = locationPart.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                request.Countries.Add(segments[0]);
            }
            for (var i = 1; i < segments.Length; i++)
            {
                request.Cities.Add(segments[i]);
            }
            text = text[..locationMarker];
        }

        var lower = text.ToLowerInvariant();
        var mustHaveAll = lower.Contains(" and ") || lower.Contains("all of") || lower.Contains(" with ");
        var mustNot = lower.Contains(" not ") || lower.Contains(" without ") || lower.Contains(" except ");

        var terms = SplitTerms(text);
        foreach (var term in terms)
        {
            if (IsLikelySkill(term))
            {
                request.Expertises.Add(term);
            }
            else if (LooksLikeCompany(term))
            {
                request.Companies.Add(term);
            }
            else
            {
                request.JobTitles.Add(term);
                request.Keywords.Add(term);
            }
        }

        if (mustNot && request.Expertises.Count > 0)
        {
            request.ExpertiseMode = ProfileSearchRequest.MustNotHaveAny;
        }
        else if (mustHaveAll && request.Expertises.Count > 1)
        {
            request.ExpertiseMode = ProfileSearchRequest.MustHaveAll;
        }

        Normalize(request);
        return request;
    }

    private static List<string> SplitTerms(string text) =>
        text.Split(new[] { ",", ";", " and ", " or ", " with ", " without ", " not ", " who " }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool IsLikelySkill(string term) =>
        SkillHints.Any(hint => term.Contains(hint, StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeCompany(string term) =>
        CompanyHints.Any(hint => term.EndsWith(hint, StringComparison.OrdinalIgnoreCase)) ||
        term.Contains("Ltd", StringComparison.OrdinalIgnoreCase) ||
        term.Contains("Inc", StringComparison.OrdinalIgnoreCase) ||
        term.Contains("Limited", StringComparison.OrdinalIgnoreCase);

    private static readonly string[] SkillHints =
    [
        "engineer", "developer", "designer", "architect", "python", "java", "c#", "javascript",
        "react", "angular", "vue", ".net", "sql", "aws", "azure", "cloud", "devops", "recruitment",
        "marketing", "salesforce", "accounting", "finance", "nurse", "teacher", "welder", "driver"
    ];

    private static readonly string[] CompanyHints = ["Corp", "Group", "Solutions", "Systems", "Technologies", "Consulting"];

    private static void Normalize(ProfileSearchRequest r)
    {
        r.JobTitleScope = ValidScope(r.JobTitleScope, "any");
        r.CompanyScope = ValidScope(r.CompanyScope, "current");
        var modes = new[]
        {
            ProfileSearchRequest.MustHaveAny, ProfileSearchRequest.MustHaveAll,
            ProfileSearchRequest.MustNotHaveAny, ProfileSearchRequest.MustNotHaveAll
        };
        if (!modes.Contains(r.ExpertiseMode, StringComparer.OrdinalIgnoreCase))
        {
            r.ExpertiseMode = ProfileSearchRequest.MustHaveAny;
        }
    }

    private static string ValidScope(string value, string fallback) =>
        value is "any" or "current" or "past" ? value : fallback;
}


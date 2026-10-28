using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutoSourcing.Core.Entities;
using AutoSourcing.Services.NLSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.ContentGeneration;

public class ContentGenerationService : IContentGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly NLSearchOptions _options;
    private readonly ILogger<ContentGenerationService> _logger;

    public ContentGenerationService(HttpClient httpClient, IOptions<NLSearchOptions> options, ILogger<ContentGenerationService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GeneratedContent> GenerateAsync(GenerateContentRequest request, Job job, OrganizationProfile? organization, CancellationToken cancellationToken = default)
    {
        GeneratedContent result;

        if (!string.IsNullOrWhiteSpace(_options.OpenAIApiKey))
        {
            try
            {
                result = await GenerateWithOpenAIAsync(request, job, organization, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI content generation failed, falling back to template.");
                result = TemplateFallback(request, job);
            }
        }
        else
        {
            result = TemplateFallback(request, job);
        }

        if (!string.IsNullOrWhiteSpace(job.AdvertUrl))
        {
            result = new GeneratedContent(result.Subject, result.Body.Replace("{{JobUrl}}", job.AdvertUrl));
        }

        return result;
    }

    private async Task<GeneratedContent> GenerateWithOpenAIAsync(GenerateContentRequest request, Job job, OrganizationProfile? organization, CancellationToken cancellationToken)
    {
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request, job, organization);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAIApiKey);
        requestMessage.Content = JsonContent.Create(new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.7
        });

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        var content = completion.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";

        var parsed = JsonSerializer.Deserialize<GeneratedContentDto>(content, JsonOptions);
        return new GeneratedContent(parsed?.Subject ?? "Job opportunity", parsed?.Body ?? "");
    }

    private static string BuildSystemPrompt() =>
        """
        You are an expert recruitment copywriter. Write professional, engaging outreach messages to candidates.

        Rules:
        - Use personalisation tokens: {{FirstName}}, {{LastName}}, {{Company}}, {{JobTitle}}, {{Location}}
        - Always include a link to the job using the token {{JobUrl}} — place it naturally in the body (e.g. "You can view the full role here: {{JobUrl}}")
        - For the FIRST outreach: include a consent call-to-action where the candidate can confirm they are happy to be contacted. Use the token {{ConsentUrl}} as the link (e.g. "Click here to confirm you're happy to hear from us: {{ConsentUrl}}")
        - Keep tone professional but warm and human
        - For Email: include a clear call-to-action
        - For LinkedIn InMail: shorter and more conversational. IMPORTANT: the body must be under 1300 characters (LinkedIn InMail limit for non-connections). Subject must be under 200 characters.
        - For the FIRST outreach: include a warm introduction, mention the job, and ask if they are open to hearing more.
        - For FOLLOW-UP messages: reference the previous outreach gently, add new value or a different angle, keep it concise
        - The body should be HTML (use <p> for paragraphs, <strong> for emphasis, <ul>/<li> for lists)
        - Do NOT include <html>, <head>, or <body> tags — just the content
        - Keep the body under 200 words for the first message, 150 for follow-ups (shorter for LinkedIn)
        - Return ONLY JSON: { "subject": "subject line", "body": "HTML body" }
        """;

    private static string BuildUserPrompt(GenerateContentRequest request, Job job, OrganizationProfile? organization)
    {
        var channel = request.Channel.ToLowerInvariant() switch
        {
            "linkedin" or "3" => "LinkedIn InMail",
            _ => "Email"
        };

        var stepContext = request.StepNumber <= 1
            ? "This is the FIRST outreach in the sequence."
            : $"This is FOLLOW-UP message number {request.StepNumber} ({request.StepName}).";

        var goal = string.IsNullOrWhiteSpace(request.Goal) ? "" : $"\nCampaign goal: {request.Goal}";
        var extra = string.IsNullOrWhiteSpace(request.AdditionalInstructions) ? "" : $"\nAdditional instructions: {request.AdditionalInstructions}";

        var orgSection = "";
        if (organization is not null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(StripHtml(organization.About))) parts.Add($"About the company: {StripHtml(organization.About)}");
            if (!string.IsNullOrWhiteSpace(StripHtml(organization.EVP))) parts.Add($"EVP / Why join: {StripHtml(organization.EVP)}");
            if (!string.IsNullOrWhiteSpace(StripHtml(organization.Culture))) parts.Add($"Culture & benefits: {StripHtml(organization.Culture)}");
            if (parts.Count > 0)
            {
                orgSection = "\n\nCompany context:\n" + string.Join("\n", parts);
            }
        }

        var jobUrl = job.AdvertUrl ?? "Not provided";
        var jobUrlToken = "{{JobUrl}}";
        var orgNameToken = "{{OrgName}}";

        return $"""
            Write a {channel} outreach message. {stepContext}{goal}{extra}

            Job details:
            - Title: {job.Title}
            - Location: {job.Location ?? "Not specified"}
            - Industry: {job.Industry ?? "Not specified"}
            - Key requirements: {job.MustHaves ?? "Not specified"}
            - Skills: {job.Skills ?? "Not specified"}
            - Why the role is attractive: {job.AttractiveReasons ?? "Not specified"}
            - Job URL: {jobUrl}
            {orgSection}

            Generate a subject line and HTML body. Use personalisation tokens where natural. Always include the job link using the token {jobUrlToken}. Use the token {orgNameToken} where you would mention the organization name.
            """;
    }

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
    }

    private static GeneratedContent TemplateFallback(GenerateContentRequest request, Job job)
    {
        var channel = request.Channel.ToLowerInvariant() switch
        {
            "linkedin" or "3" => "LinkedIn",
            _ => "Email"
        };

        var jobLink = string.IsNullOrWhiteSpace(job.AdvertUrl)
            ? ""
            : " <a href=\"" + job.AdvertUrl + "\">View the full role here</a>";

        if (request.StepNumber <= 1)
        {
            return new GeneratedContent(
                "Job opportunity: " + job.Title,
                "<p>Hi {{FirstName}},</p><p>I came across your profile and thought you'd be a great fit for a <strong>" + job.Title + "</strong> role" + (string.IsNullOrWhiteSpace(job.Location) ? "" : " in " + job.Location) + ".</p><p>" + (job.AttractiveReasons ?? "This is a great opportunity to grow your career.") + "</p><p>Would you be open to hearing more? Just reply to confirm you're happy to be contacted about this role.</p><p>" + jobLink + "</p><p>Best regards</p>");
        }

        return new GeneratedContent(
            "Following up: " + job.Title,
            "<p>Hi {{FirstName}},</p><p>Just following up on my previous message about the <strong>" + job.Title + "</strong> role. I'd love to hear if you're interested.</p><p>" + jobLink + "</p><p>Happy to answer any questions.</p><p>Best regards</p>");
    }

    private record GeneratedContentDto(string? Subject, string? Body);
}

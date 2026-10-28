using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutoSourcing.Core.Entities;
using AutoSourcing.Services.NLSearch;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Interviews;

public record InterviewInsights(
    string Summary,
    string FitAgainstRole,
    string MissingInformation,
    IReadOnlyList<string> FollowUpQuestions,
    string Html);

public interface IInterviewSummaryService
{
    Task<InterviewInsights?> AnalyseAsync(string transcript, string candidateName, Job? job, CancellationToken cancellationToken = default);
}

public class InterviewSummaryService : IInterviewSummaryService
{
    private const int MaxTranscriptChars = 60_000;

    private readonly HttpClient _httpClient;
    private readonly NLSearchOptions _options;

    public InterviewSummaryService(HttpClient httpClient, IOptions<NLSearchOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _options = options.Value;
    }

    public async Task<InterviewInsights?> AnalyseAsync(string transcript, string candidateName, Job? job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OpenAIApiKey) || string.IsNullOrWhiteSpace(transcript))
        {
            return null;
        }

        var text = transcript.Length > MaxTranscriptChars ? transcript[..MaxTranscriptChars] : transcript;

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAIApiKey);
        requestMessage.Content = JsonContent.Create(new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = BuildUserPrompt(candidateName, job, text) }
            },
            response_format = new { type = "json_object" },
            temperature = 0.2
        });

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        using var parsed = JsonDocument.Parse(content);
        var root = parsed.RootElement;

        var summary = GetString(root, "summary");
        var fit = GetString(root, "fitAgainstRole");
        var missing = GetString(root, "missingInformation");

        var questions = new List<string>();
        if (root.TryGetProperty("followUpQuestions", out var questionsElement) && questionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in questionsElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                {
                    questions.Add(item.GetString()!);
                }
            }
        }

        return new InterviewInsights(
            summary,
            fit,
            missing,
            questions,
            BuildHtml(candidateName, summary, fit, missing, questions));
    }

    private const string SystemPrompt =
        "You are an expert recruitment assistant. You read interview transcripts for a hiring manager. " +
        "Be concise, factual and neutral. Only use information that appears in the transcript - never invent anything. " +
        "Return JSON only.";

    private static string BuildUserPrompt(string candidateName, Job? job, string transcript)
    {
        static string Or(string? value) => string.IsNullOrWhiteSpace(value) ? "not specified" : value!;

        var jobSummary = job is null
            ? "No specific role has been linked to this interview."
            : $"Role: {Or(job.Title)}\n" +
              $"Location: {Or(job.Location)}\n" +
              $"Must-haves: {Or(job.MustHaves)}\n" +
              $"Nice-to-haves: {Or(job.NiceToHaves)}\n" +
              $"Skills: {Or(job.Skills)}\n" +
              $"Education: {Or(job.Education)}\n" +
              $"Screening / process details: {Or(job.ScreeningDetails)}";

        return
            $"Candidate: {candidateName}\n\n" +
            jobSummary + "\n\n" +
            "Interview transcript (WebVTT):\n" + transcript + "\n\n" +
            "Produce JSON with exactly these fields:\n" +
            "{\n" +
            "  \"summary\": \"3-6 sentence factual summary of the interview, including what the candidate said about their experience, motivation and availability.\",\n" +
            "  \"fitAgainstRole\": \"How the candidate's answers line up with the role requirements above. Call out clear matches and any concerns, quoting the candidate where useful.\",\n" +
            "  \"missingInformation\": \"What the interviewer still does not know that matters for this role (experience, salary expectations, notice period, right to work, availability, and so on).\",\n" +
            "  \"followUpQuestions\": [\"3 to 6 specific follow-up questions to fill the gaps you identified\"]\n" +
            "}";
    }

    private static string GetString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string BuildHtml(string candidateName, string summary, string fit, string missing, IReadOnlyList<string> questions)
    {
        var builder = new StringBuilder();
        builder.Append($"<h3>Interview summary — {WebUtility.HtmlEncode(candidateName)}</h3>");
        Append(builder, "Summary", summary);
        Append(builder, "Fit against the role", fit);
        Append(builder, "Missing information", missing);

        if (questions.Count > 0)
        {
            builder.Append("<p><strong>Suggested follow-up questions</strong></p><ul>");
            foreach (var question in questions)
            {
                builder.Append($"<li>{WebUtility.HtmlEncode(question)}</li>");
            }

            builder.Append("</ul>");
        }

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string heading, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var encoded = WebUtility.HtmlEncode(value).Replace("\n", "<br/>");
        builder.Append($"<p><strong>{heading}</strong><br/>{encoded}</p>");
    }
}

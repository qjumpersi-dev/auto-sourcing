using AutoSourcing.Core.Entities;

namespace AutoSourcing.Services.ContentGeneration;

public record GeneratedContent(string Subject, string Body);

public class GenerateContentRequest
{
    public int JobId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Channel { get; set; } = "Email";
    public int StepNumber { get; set; } = 1;
    public string? Goal { get; set; }
    public string? AdditionalInstructions { get; set; }
}

public interface IContentGenerationService
{
    Task<GeneratedContent> GenerateAsync(GenerateContentRequest request, Job job, OrganizationProfile? organization, CancellationToken cancellationToken = default);
}

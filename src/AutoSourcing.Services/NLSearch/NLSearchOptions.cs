namespace AutoSourcing.Services.NLSearch;

public class NLSearchOptions
{
    public const string SectionName = "NLSearch";

    public string? OpenAIApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
}

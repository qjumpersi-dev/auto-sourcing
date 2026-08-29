using System.Text.Json.Serialization;

namespace AutoSourcing.Services.Scotty;

public class ScottyChatRequest
{
    public string UserPrompt { get; set; } = string.Empty;
    public string ContinuityKey { get; set; } = string.Empty;
}

public class ScottyChatResponse
{
    public string? Output { get; set; }
    public List<ScottyAttachment> Attachments { get; set; } = [];
    public ScottyMetadata? Metadata { get; set; }
}

public class ScottyAttachment
{
    public string? Url { get; set; }

    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }

    public string? Caption { get; set; }
}

public class ScottyMetadata
{
    [JsonPropertyName("platform_session_id")]
    public string? PlatformSessionId { get; set; }

    [JsonPropertyName("continuity_key")]
    public string? ContinuityKey { get; set; }

    [JsonPropertyName("agent_instance_id")]
    public string? AgentInstanceId { get; set; }

    [JsonPropertyName("agent_definition_id")]
    public string? AgentDefinitionId { get; set; }

    [JsonPropertyName("routing_rule_id")]
    public string? RoutingRuleId { get; set; }

    [JsonPropertyName("pipeline_definition_id")]
    public string? PipelineDefinitionId { get; set; }

    [JsonPropertyName("pipeline_instance_id")]
    public string? PipelineInstanceId { get; set; }
}

public class ScottyCallRequest
{
    public string SessionParticipantId { get; set; } = string.Empty;
    public string? ContinuityKey { get; set; }
}

public class ScottyCallResponse
{
    public string? Url { get; set; }
    public string? Token { get; set; }
    public ScottyMetadata? Metadata { get; set; }
}
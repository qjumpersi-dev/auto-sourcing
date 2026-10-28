using System.Text.Json;
using System.Text.Json.Nodes;
using AutoSourcing.Core.Enums;
using AutoSourcing.Services.Agent;
using AutoSourcing.Services.Qualification;
using Microsoft.AspNetCore.Mvc;

namespace AutoSourcing.API.Controllers;

[ApiController]
[Route("api/mcp")]
public class McpController : ControllerBase
{
    private const string ProtocolVersion = "2024-11-05";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICandidateAgentService _agentService;
    private readonly ICandidateQualificationService _qualificationService;
    private readonly ILogger<McpController> _logger;

    public McpController(
        ICandidateAgentService agentService,
        ICandidateQualificationService qualificationService,
        ILogger<McpController> logger)
    {
        _agentService = agentService;
        _qualificationService = qualificationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        string? method = null;
        JsonElement id = default;
        var hasId = false;

        if (body.ValueKind == JsonValueKind.Object)
        {
            if (body.TryGetProperty("method", out var methodProp))
            {
                method = methodProp.GetString();
            }
            if (body.TryGetProperty("id", out var idProp))
            {
                id = idProp;
                hasId = true;
            }
        }

        // Notifications have no id and expect no response.
        if (!hasId)
        {
            return Accepted();
        }

        var result = method switch
        {
            "initialize" => HandleInitialize(),
            "tools/list" => HandleToolsList(),
            "tools/call" => await HandleToolsCallAsync(body, cancellationToken),
            _ => null
        };

        if (result is null)
        {
            return Ok(ErrorResponse(id, -32601, $"Method not found: {method}"));
        }

        return Ok(SuccessResponse(id, result));
    }

    /// <summary>
    /// Scotty passes the continuity key in the W3C `baggage` header. Fall back to an
    /// explicit tool argument if provided.
    /// </summary>
    private Guid? ResolveContinuityKey(string? argumentValue)
    {
        if (Guid.TryParse(argumentValue, out var fromArg))
        {
            return fromArg;
        }

        var baggage = Request.Headers["baggage"].ToString();
        if (!string.IsNullOrWhiteSpace(baggage))
        {
            foreach (var item in baggage.Split(','))
            {
                var pair = item.Split(';')[0];
                var eq = pair.IndexOf('=');
                if (eq <= 0) continue;

                var key = pair[..eq].Trim();
                if (key.Contains("continuity", StringComparison.OrdinalIgnoreCase))
                {
                    var value = Uri.UnescapeDataString(pair[(eq + 1)..].Trim());
                    if (Guid.TryParse(value, out var fromBaggage))
                    {
                        return fromBaggage;
                    }
                }
            }
        }

        return null;
    }

    private static JsonObject HandleInitialize()
    {
        return new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject()
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "aits-recruitment-agent",
                ["version"] = "1.0.0"
            }
        };
    }

    private static JsonObject HandleToolsList()
    {
        var tools = new JsonArray
        {
            Tool("get_candidate_context",
                "Get the candidate's name, contact details, current company and job title. Call this first.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject()
                }),
            Tool("get_company_info",
                "Get information about the hiring company: about, EVP, culture, and hiring process.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject()
                }),
            Tool("get_role_details",
                "Get the role the candidate is being considered for: must-haves, skills, key requirements, hiring manager, location, salary and why to join. Call this when the candidate asks about the job.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject()
                }),
            Tool("get_guardrails",
                "Get the policy guardrails: what you may answer, escalation triggers, refusal topics, disclaimers, and market rules.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject()
                }),
            Tool("escalate_to_recruiter",
                "Escalate the conversation to a human recruiter when the candidate asks for a human, raises compensation/visa/legal topics, complains, or you are uncertain.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["reason"] = new JsonObject { ["type"] = "string", ["description"] = "Why the conversation is being escalated." }
                    },
                    ["required"] = new JsonArray("reason")
                }),
            Tool("save_note",
                "Save a note about the conversation against the candidate record.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["content"] = new JsonObject { ["type"] = "string", ["description"] = "The note to save." }
                    },
                    ["required"] = new JsonArray("content")
                }),
            Tool("get_interview",
                "Get the candidate's currently booked interview, if any.",
                new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() }),
            Tool("get_interview_slots",
                "Get the next available interview times to offer the candidate. Call this when the candidate is interested in moving forward.",
                new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() }),
            Tool("book_interview",
                "Book the interview for the option number the candidate chose. Call get_interview_slots first.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["slot"] = new JsonObject { ["type"] = "integer", ["description"] = "The option number the candidate chose (1, 2 or 3)." }
                    },
                    ["required"] = new JsonArray("slot")
                }),
            Tool("reschedule_interview",
                "Move an existing booked interview to a new option number. Call get_interview_slots first.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["slot"] = new JsonObject { ["type"] = "integer", ["description"] = "The new option number the candidate chose." }
                    },
                      ["required"] = new JsonArray("slot")
                  }),
            Tool("get_interview_ready_checklist",
                "Get the candidate's interview-ready checklist: the fixed checks, the question to ask, the expected answer from the job brief, and the current status. Call this before qualifying a candidate.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject()
                }),
            Tool("save_checklist_answer",
                "Record the candidate's answer for one interview-ready check. Use the criterion name from get_interview_ready_checklist.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["criterion"] = new JsonObject { ["type"] = "string", ["description"] = "The check, e.g. role_relevance, work_hours, location, pay, work_authorisation, background_checks, licences, interest." },
                        ["status"] = new JsonObject { ["type"] = "string", ["description"] = "met, not_met, not_applicable or unknown." },
                        ["answer"] = new JsonObject { ["type"] = "string", ["description"] = "What the candidate said." },
                        ["notes"] = new JsonObject { ["type"] = "string", ["description"] = "Optional notes." }
                    },
                    ["required"] = new JsonArray("criterion", "status")
                })
          };

        return new JsonObject { ["tools"] = tools };
    }

    private async Task<JsonObject> HandleToolsCallAsync(JsonElement body, CancellationToken cancellationToken)
    {
        if (!body.TryGetProperty("params", out var paramsProp))
        {
            return TextResult("Missing params.");
        }

        var name = paramsProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        var args = paramsProp.TryGetProperty("arguments", out var argsProp) ? argsProp : default;

        var text = await ExecuteToolAsync(name, args, cancellationToken);
        return TextResult(text);
    }

    /// <summary>
    /// Direct tool execution endpoint matching Scotty's `POST /tools/execute` shape:
    /// { "tool": "name", "args": { ... } }.
    /// </summary>
    [HttpPost("tools/execute")]
    public async Task<IActionResult> ExecuteTool([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var name = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("tool", out var toolProp)
            ? toolProp.GetString()
            : null;
        var args = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("args", out var argsProp)
            ? argsProp
            : default;

        _logger.LogInformation(
            "MCP /tools/execute: {Tool}. Baggage: {Baggage}. SessionId: {SessionId}",
            name,
            Request.Headers["baggage"].ToString(),
            Request.Headers["P-API-Platform-Session-ID"].ToString());

        var result = await ExecuteToolAsync(name, args, cancellationToken);
        return Ok(new JsonObject
        {
            ["tool"] = name,
            ["result"] = result
        });
    }

    private async Task<string> ExecuteToolAsync(string? name, JsonElement args, CancellationToken cancellationToken)
    {
        string? GetArg(string key)
        {
            if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty(key, out var value))
            {
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
            }
            return null;
        }

        Guid? GetKey()
        {
            return ResolveContinuityKey(GetArg("continuity_key"));
        }

        switch (name)
        {
            case "get_candidate_context":
            {
                var key = GetKey();
                if (key is null) return "Invalid or missing continuity key.";
                var context = await _agentService.GetContextAsync(key.Value, cancellationToken);
                if (context is null) return "Candidate not found.";
                return $"""
                    Candidate: {context.CandidateName}
                    Email: {context.Email ?? "—"}
                    Phone: {context.Phone ?? "—"}
                    Current company: {context.Company ?? "—"}
                    Current job title: {context.JobTitle ?? "—"}
                    Location: {context.Location ?? context.Country ?? "—"}
                    """;
            }

            case "get_company_info":
                return await _agentService.GetCompanyInfoAsync(GetKey(), cancellationToken);

            case "get_role_details":
            {
                var key = GetKey();
                if (key is null) return "Invalid or missing continuity key.";
                return await _agentService.GetRoleDetailsAsync(key.Value, cancellationToken);
            }

            case "get_guardrails":
                return await _agentService.GetGuardrailsAsync(GetKey(), cancellationToken);

            case "escalate_to_recruiter":
            {
                var key = GetKey();
                var reason = GetArg("reason") ?? "Candidate requested a human.";
                if (key is null) return "Invalid or missing continuity key.";
                var ok = await _agentService.EscalateAsync(key.Value, reason, cancellationToken);
                return ok
                    ? "Escalation recorded. A recruiter will follow up."
                    : "Candidate not found; escalation not recorded.";
            }

            case "save_note":
            {
                var key = GetKey();
                var content = GetArg("content") ?? string.Empty;
                if (key is null) return "Invalid or missing continuity key.";
                var lead = await _agentService.ResolveLeadAsync(key.Value, cancellationToken);
                if (lead is null) return "Candidate not found.";
                await _agentService.SaveMessageAsync(lead.Id, "agent-note", content, cancellationToken: cancellationToken);
                return "Note saved.";
            }

            case "get_interview":
            {
                var key = GetKey();
                if (key is null) return "Invalid or missing continuity key.";
                return await _agentService.GetInterviewAsync(key.Value, cancellationToken);
            }

            case "get_interview_slots":
            {
                var key = GetKey();
                if (key is null) return "Invalid or missing continuity key.";
                return await _agentService.GetInterviewSlotsAsync(key.Value, cancellationToken);
            }

            case "book_interview":
            {
                var key = GetKey();
                if (key is null) return "Invalid or missing continuity key.";
                if (!int.TryParse(GetArg("slot"), out var slot)) return "slot must be a number (1, 2 or 3).";
                return await _agentService.BookInterviewAsync(key.Value, slot, cancellationToken);
            }

            case "reschedule_interview":
            {
                var key = GetKey();
                if (key is null) return "Invalid or missing continuity key.";
                if (!int.TryParse(GetArg("slot"), out var slot)) return "slot must be a number (1, 2 or 3).";
                return await _agentService.RescheduleInterviewAsync(key.Value, slot, cancellationToken);
            }

            case "get_interview_ready_checklist":
            {
                var key = GetKey();
                if (key is null) return "Invalid or missing continuity key.";
                var lead = await _agentService.ResolveLeadAsync(key.Value, cancellationToken);
                if (lead is null) return "Candidate not found.";

                var qualification = await _qualificationService.GetOrCreateAsync(lead.Id, null, null, cancellationToken);
                var lines = qualification.Checks
                    .OrderBy(c => c.Criterion)
                    .Select(c =>
                        $"- {CandidateQualificationService.CriterionLabel(c.Criterion)} ({CriterionSlug(c.Criterion)}): " +
                        $"{c.Prompt}" +
                        (string.IsNullOrWhiteSpace(c.Expected) ? "" : $" Expected: {c.Expected}") +
                        $" [status: {c.Status}]");
                return "Interview-ready checklist:\n" + string.Join("\n", lines);
            }

            case "save_checklist_answer":
            {
                var key = GetKey();
                if (key is null) return "Invalid or missing continuity key.";
                var lead = await _agentService.ResolveLeadAsync(key.Value, cancellationToken);
                if (lead is null) return "Candidate not found.";

                var criterion = ParseCriterion(GetArg("criterion"));
                if (criterion is null) return "Unknown criterion. Use a name from get_interview_ready_checklist.";

                var status = ParseStatus(GetArg("status"));
                var qualification = await _qualificationService.GetOrCreateAsync(lead.Id, null, null, cancellationToken);
                var check = qualification.Checks.FirstOrDefault(c => c.Criterion == criterion.Value);
                if (check is null) return "That check was not found for this candidate.";

                await _qualificationService.UpdateCheckAsync(
                    check.Id,
                    status,
                    GetArg("answer"),
                    GetArg("notes"),
                    CheckSource.Agent,
                    cancellationToken);

                return $"Saved {CandidateQualificationService.CriterionLabel(criterion.Value)} as {status}.";
            }

            default:
                return $"Unknown tool: {name}";
        }
    }

    private static string CriterionSlug(InterviewReadyCriterion criterion)
    {
        // RoleRelevance -> role_relevance
        var name = criterion.ToString();
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                sb.Append('_');
            }
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }

    private static InterviewReadyCriterion? ParseCriterion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalised = new string(value.Where(char.IsLetter).ToArray()).ToLowerInvariant();
        foreach (var criterion in Enum.GetValues<InterviewReadyCriterion>())
        {
            if (new string(criterion.ToString().Where(char.IsLetter).ToArray()).ToLowerInvariant() == normalised)
            {
                return criterion;
            }
        }
        return null;
    }

    private static InterviewReadyStatus ParseStatus(string? value)
    {
        var normalised = (value ?? string.Empty).Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
        return normalised switch
        {
            "met" or "yes" or "true" => InterviewReadyStatus.Met,
            "not_met" or "notmet" or "no" or "false" or "not_suitable" => InterviewReadyStatus.NotMet,
            "not_applicable" or "notapplicable" or "n/a" or "na" => InterviewReadyStatus.NotApplicable,
            _ => InterviewReadyStatus.Unknown
        };
    }

    private static JsonObject Tool(string name, string description, JsonObject inputSchema)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["description"] = description,
            ["inputSchema"] = inputSchema
        };
    }

    private static JsonObject TextResult(string text)
    {
        return new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = text
                }
            }
        };
    }

    private static JsonObject SuccessResponse(JsonElement id, JsonNode result)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNode.Parse(id.GetRawText()),
            ["result"] = result
        };
    }

    private static JsonObject ErrorResponse(JsonElement id, int code, string message)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNode.Parse(id.GetRawText()),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
    }
}

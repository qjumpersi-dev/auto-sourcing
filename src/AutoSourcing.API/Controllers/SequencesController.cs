using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Services.Outreach;
using Microsoft.AspNetCore.Mvc;

namespace AutoSourcing.API.Controllers;

public class SequenceStepDto
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public OutreachChannel Channel { get; set; }
    public string? SubjectTemplate { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public int DelayDays { get; set; }
    public SequenceStepCondition Condition { get; set; }
}

public class SequenceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SequenceStatus Status { get; set; }
    public int SendDaysMask { get; set; }
    public TimeOnly SendWindowStart { get; set; }
    public TimeOnly SendWindowEnd { get; set; }
    public bool IncludeUnsubscribe { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<SequenceStepDto> Steps { get; set; } = [];
}

public class SequencePreviewRequest
{
    public int LeadId { get; set; }
    public string? SubjectTemplate { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public OutreachChannel Channel { get; set; } = OutreachChannel.Email;
    public bool IncludeUnsubscribe { get; set; } = true;
    public int? JobId { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class SequencesController : ControllerBase
{
    private readonly ISequenceService _sequenceService;

    public SequencesController(ISequenceService sequenceService)
    {
        _sequenceService = sequenceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SequenceDto>>> GetSequences(CancellationToken cancellationToken)
    {
        var sequences = await _sequenceService.GetSequencesAsync(cancellationToken);
        return Ok(sequences.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SequenceDto>> GetSequence(int id, CancellationToken cancellationToken)
    {
        var sequence = await _sequenceService.GetSequenceAsync(id, cancellationToken);
        return sequence is null ? NotFound() : Ok(MapToDto(sequence));
    }

    [HttpPost]
    public async Task<ActionResult<SequenceDto>> CreateSequence([FromBody] SaveSequenceRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return BadRequest(new { error = validation });
        }

        var sequence = await _sequenceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSequence), new { id = sequence.Id }, MapToDto(sequence));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SequenceDto>> UpdateSequence(int id, [FromBody] SaveSequenceRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return BadRequest(new { error = validation });
        }

        var sequence = await _sequenceService.UpdateAsync(id, request, cancellationToken);
        return sequence is null ? NotFound() : Ok(MapToDto(sequence));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSequence(int id, CancellationToken cancellationToken)
    {
        var deleted = await _sequenceService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("personalisation-options")]
    public ActionResult<IEnumerable<PersonalisationOption>> GetPersonalisationOptions()
    {
        return Ok(_sequenceService.GetPersonalisationOptions());
    }

    [HttpPost("preview")]
    public async Task<ActionResult<SequencePreview>> Preview([FromBody] SequencePreviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _sequenceService.PreviewAsync(
                request.LeadId,
                request.SubjectTemplate,
                request.BodyTemplate,
                request.Channel,
                request.IncludeUnsubscribe,
                request.JobId,
                cancellationToken);

            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static string? Validate(SaveSequenceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "A sequence name is required.";
        }

        if (request.Steps.Count == 0)
        {
            return "Add at least one outreach step.";
        }

        if (request.SendDaysMask == 0)
        {
            return "Select at least one day of the week to send.";
        }

        if (request.SendWindowEnd <= request.SendWindowStart)
        {
            return "The send window end time must be after the start time.";
        }

        for (var i = 0; i < request.Steps.Count; i++)
        {
            var step = request.Steps[i];
            if (string.IsNullOrWhiteSpace(step.SubjectTemplate))
            {
                return $"Step {i + 1} needs a subject.";
            }

            if (string.IsNullOrWhiteSpace(step.BodyTemplate))
            {
                return $"Step {i + 1} needs a message body.";
            }
        }

        return null;
    }

    private static SequenceDto MapToDto(Sequence sequence) => new()
    {
        Id = sequence.Id,
        Name = sequence.Name,
        Description = sequence.Description,
        Status = sequence.Status,
        SendDaysMask = sequence.SendDaysMask,
        SendWindowStart = sequence.SendWindowStart,
        SendWindowEnd = sequence.SendWindowEnd,
        IncludeUnsubscribe = sequence.IncludeUnsubscribe,
        CreatedAt = sequence.CreatedAt,
        UpdatedAt = sequence.UpdatedAt,
        Steps = sequence.Steps
            .OrderBy(s => s.Order)
            .Select(s => new SequenceStepDto
            {
                Id = s.Id,
                Order = s.Order,
                Name = s.Name,
                Channel = s.Channel,
                SubjectTemplate = s.SubjectTemplate,
                BodyTemplate = s.BodyTemplate,
                DelayDays = s.DelayDays,
                Condition = s.Condition
            })
            .ToList()
    };
}

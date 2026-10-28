using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Services.Qualification;
using Microsoft.AspNetCore.Mvc;

namespace AutoSourcing.API.Controllers;

public class UpdateQualificationRequest
{
    public string? PayExpectation { get; set; }
    public string? Availability { get; set; }
    public string? NoticePeriod { get; set; }
}

public class UpdateCheckRequest
{
    public InterviewReadyStatus Status { get; set; }
    public string? Answer { get; set; }
    public string? Notes { get; set; }
    public CheckSource? Source { get; set; }
}

public class HandoverSentRequest
{
    public string? To { get; set; }
}

[ApiController]
[Route("api")]
public class CandidateQualificationController : ControllerBase
{
    private readonly ICandidateQualificationService _qualificationService;
    private readonly ICurrentUserContext? _currentUser;

    public CandidateQualificationController(
        ICandidateQualificationService qualificationService,
        ICurrentUserContext? currentUser = null)
    {
        _qualificationService = qualificationService;
        _currentUser = currentUser;
    }

    [HttpGet("leads/{leadId:int}/qualification")]
    public async Task<ActionResult> Get(int leadId, [FromQuery] int? jobId, [FromQuery] int? campaignId, CancellationToken cancellationToken)
    {
        var qualification = await _qualificationService.GetOrCreateAsync(leadId, jobId, campaignId, cancellationToken);
        return Ok(ToDto(qualification));
    }

    [HttpPut("qualifications/{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateQualificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var qualification = await _qualificationService.UpdateHeaderAsync(
                id, request.PayExpectation, request.Availability, request.NoticePeriod, cancellationToken);
            return Ok(ToDto(qualification));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("qualifications/checks/{checkId:int}")]
    public async Task<ActionResult> UpdateCheck(int checkId, [FromBody] UpdateCheckRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var check = await _qualificationService.UpdateCheckAsync(
                checkId, request.Status, request.Answer, request.Notes, request.Source ?? CheckSource.Human, cancellationToken);
            return Ok(ToCheckDto(check));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("qualifications/checks/{checkId:int}/confirm")]
    public async Task<ActionResult> ConfirmCheck(int checkId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUser?.UserId ?? 0;
            var check = await _qualificationService.ConfirmCheckAsync(checkId, userId, cancellationToken);
            return Ok(ToCheckDto(check));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("qualifications/{id:int}/handover")]
    public async Task<ActionResult> Handover(int id, CancellationToken cancellationToken)
    {
        try
        {
            var html = await _qualificationService.BuildHandoverAsync(id, cancellationToken);
            return Ok(new { html });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("qualifications/{id:int}/handover-sent")]
    public async Task<ActionResult> HandoverSent(int id, [FromBody] HandoverSentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var qualification = await _qualificationService.MarkHandoverSentAsync(id, request.To, cancellationToken);
            return Ok(new { sentAt = qualification.HandoverSentAt, to = qualification.HandoverSentTo });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private static object ToDto(CandidateQualification qualification)
    {
        var checks = qualification.Checks.OrderBy(c => c.Criterion).ToList();
        var agentCriteria = checks.Where(c => c.Criterion != InterviewReadyCriterion.HumanConfirmation && c.Criterion != InterviewReadyCriterion.InterviewArranged).ToList();
        var agentComplete = agentCriteria.Count > 0 && agentCriteria.All(IsSatisfied);
        var interviewReady = checks.Count > 0 && checks.All(IsSatisfied);

        return new
        {
            qualification.Id,
            qualification.LeadId,
            qualification.JobId,
            qualification.CampaignId,
            qualification.PayExpectation,
            qualification.Availability,
            qualification.NoticePeriod,
            qualification.ConfirmedByUserId,
            qualification.ConfirmedAt,
            qualification.HandoverSentAt,
            qualification.HandoverSentTo,
            AgentComplete = agentComplete,
            InterviewReady = interviewReady,
            Checks = checks.Select(ToCheckDto)
        };
    }

    private static bool IsSatisfied(QualificationCheck check) =>
        check.Status is InterviewReadyStatus.Met or InterviewReadyStatus.NotApplicable;

    private static object ToCheckDto(QualificationCheck check) => new
    {
        check.Id,
        check.CandidateQualificationId,
        Criterion = check.Criterion.ToString(),
        CriterionLabel = CandidateQualificationService.CriterionLabel(check.Criterion),
        check.Prompt,
        check.Expected,
        Status = check.Status.ToString(),
        StatusLabel = CandidateQualificationService.StatusLabel(check.Status),
        check.Answer,
        check.Notes,
        Source = check.Source.ToString(),
        check.ConfirmedByUserId,
        check.ConfirmedAt,
        check.UpdatedAt
    };
}
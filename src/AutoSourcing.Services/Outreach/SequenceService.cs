using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using AutoSourcing.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Services.Outreach;

public class SequenceService : ISequenceService
{
    private readonly AutoSourcingDbContext _dbContext;
    private readonly IPersonalizationService _personalization;
    private readonly IUnsubscribeService _unsubscribeService;

    public SequenceService(
        AutoSourcingDbContext dbContext,
        IPersonalizationService personalization,
        IUnsubscribeService unsubscribeService)
    {
        _dbContext = dbContext;
        _personalization = personalization;
        _unsubscribeService = unsubscribeService;
    }

    public async Task<List<Sequence>> GetSequencesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sequences
            .Include(s => s.Steps)
            .AsNoTracking()
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Sequence?> GetSequenceAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sequences
            .Include(s => s.Steps)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sequence> CreateAsync(SaveSequenceRequest request, CancellationToken cancellationToken = default)
    {
        var sequence = new Sequence
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Status = request.Status,
            SendDaysMask = request.SendDaysMask,
            SendWindowStart = request.SendWindowStart,
            SendWindowEnd = request.SendWindowEnd,
            IncludeUnsubscribe = request.IncludeUnsubscribe,
            CreatedAt = DateTime.UtcNow
        };

        ApplySteps(sequence, request.Steps);

        _dbContext.Sequences.Add(sequence);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return sequence;
    }

    public async Task<Sequence?> UpdateAsync(int id, SaveSequenceRequest request, CancellationToken cancellationToken = default)
    {
        var sequence = await _dbContext.Sequences
            .Include(s => s.Steps)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sequence is null)
        {
            return null;
        }

        sequence.Name = request.Name.Trim();
        sequence.Description = request.Description;
        sequence.Status = request.Status;
        sequence.SendDaysMask = request.SendDaysMask;
        sequence.SendWindowStart = request.SendWindowStart;
        sequence.SendWindowEnd = request.SendWindowEnd;
        sequence.IncludeUnsubscribe = request.IncludeUnsubscribe;
        sequence.UpdatedAt = DateTime.UtcNow;

        _dbContext.SequenceSteps.RemoveRange(sequence.Steps);
        sequence.Steps.Clear();
        ApplySteps(sequence, request.Steps);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return sequence;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var sequence = await _dbContext.Sequences.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sequence is null)
        {
            return false;
        }

        _dbContext.Sequences.Remove(sequence);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public IReadOnlyList<PersonalisationOption> GetPersonalisationOptions() => _personalization.GetOptions();

    public async Task<SequencePreview> PreviewAsync(
        int leadId,
        string? subjectTemplate,
        string bodyTemplate,
        OutreachChannel channel,
        bool includeUnsubscribe,
        int? jobId = null,
        CancellationToken cancellationToken = default)
    {
        var lead = await _dbContext.Leads.FindAsync([leadId], cancellationToken)
            ?? throw new InvalidOperationException($"Lead {leadId} not found.");

        var orgName = await _dbContext.OrganizationProfiles.AsNoTracking().Select(o => o.OrgName).FirstOrDefaultAsync(cancellationToken);

        string? jobUrl = null;
        string? jobLocation = null;
        if (jobId is int jid)
        {
            var job = await _dbContext.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jid, cancellationToken);
            if (job is not null)
            {
                jobUrl = job.AdvertUrl;
                jobLocation = job.Location;
            }
        }

        var subject = _personalization.RenderTemplate(subjectTemplate ?? string.Empty, lead, jobUrl, orgName, jobLocation);
        var body = _personalization.RenderTemplate(bodyTemplate ?? string.Empty, lead, jobUrl, orgName, jobLocation);

        if (channel == OutreachChannel.Email && includeUnsubscribe)
        {
            body = _unsubscribeService.AppendFooter(body, lead);
        }

        return new SequencePreview(subject, body);
    }

    private static void ApplySteps(Sequence sequence, IReadOnlyList<SequenceStepRequest> steps)
    {
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            sequence.Steps.Add(new SequenceStep
            {
                Order = index,
                Name = string.IsNullOrWhiteSpace(step.Name) ? $"Step {index + 1}" : step.Name.Trim(),
                Channel = step.Channel,
                SubjectTemplate = step.SubjectTemplate,
                BodyTemplate = step.BodyTemplate,
                DelayDays = Math.Max(0, step.DelayDays),
                Condition = step.Condition,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}

using System.Text;
using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Services.Qualification;

public interface ICandidateQualificationService
{
    Task<CandidateQualification> GetOrCreateAsync(int leadId, int? jobId, int? campaignId, CancellationToken cancellationToken = default);

    Task<QualificationCheck> UpdateCheckAsync(
        int checkId,
        InterviewReadyStatus status,
        string? answer,
        string? notes,
        CheckSource source,
        CancellationToken cancellationToken = default);

    Task<QualificationCheck> ConfirmCheckAsync(int checkId, int userId, CancellationToken cancellationToken = default);

    Task<CandidateQualification> UpdateHeaderAsync(
        int id,
        string? payExpectation,
        string? availability,
        string? noticePeriod,
        CancellationToken cancellationToken = default);

    Task<string> BuildHandoverAsync(int id, CancellationToken cancellationToken = default);

    Task<CandidateQualification> MarkHandoverSentAsync(int id, string? to, CancellationToken cancellationToken = default);
}

public class CandidateQualificationService : ICandidateQualificationService
{
    private readonly AutoSourcingDbContext _dbContext;

    public CandidateQualificationService(AutoSourcingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CandidateQualification> GetOrCreateAsync(int leadId, int? jobId, int? campaignId, CancellationToken cancellationToken = default)
    {
        var lead = await _dbContext.Leads
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken)
            ?? throw new InvalidOperationException("Candidate not found.");

        var resolvedJobId = await ResolveJobIdAsync(lead.Id, jobId, campaignId, cancellationToken);
        var job = resolvedJobId is int jid
            ? await _dbContext.Jobs.IgnoreQueryFilters().FirstOrDefaultAsync(j => j.Id == jid, cancellationToken)
            : null;

        var qualification = await _dbContext.CandidateQualifications
            .IgnoreQueryFilters()
            .Include(q => q.Checks)
            .FirstOrDefaultAsync(q => q.LeadId == leadId && q.JobId == resolvedJobId, cancellationToken);

        if (qualification is null)
        {
            qualification = new CandidateQualification
            {
                UserId = lead.UserId,
                LeadId = leadId,
                JobId = resolvedJobId,
                CampaignId = campaignId
            };

            foreach (var item in InterviewReadyTemplate.Build(job))
            {
                qualification.Checks.Add(NewCheck(lead.UserId, item));
            }

            ApplyJobTags(lead, job);

            _dbContext.CandidateQualifications.Add(qualification);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return qualification;
        }

        // Add any criteria that were introduced after this record was created.
        var existing = qualification.Checks.Select(c => c.Criterion).ToHashSet();
        var added = false;
        foreach (var item in InterviewReadyTemplate.Build(job))
        {
            if (!existing.Contains(item.Criterion))
            {
                qualification.Checks.Add(NewCheck(qualification.UserId, item));
                added = true;
            }
        }

        if (ApplyJobTags(lead, job))
        {
            added = true;
        }

        if (campaignId is int campaign && qualification.CampaignId is null)
        {
            qualification.CampaignId = campaign;
            added = true;
        }

        if (added)
        {
            qualification.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return qualification;
    }

    public async Task<QualificationCheck> UpdateCheckAsync(
        int checkId,
        InterviewReadyStatus status,
        string? answer,
        string? notes,
        CheckSource source,
        CancellationToken cancellationToken = default)
    {
        var check = await _dbContext.QualificationChecks
            .IgnoreQueryFilters()
            .Include(c => c.Qualification)
                .ThenInclude(q => q.Lead)
            .FirstOrDefaultAsync(c => c.Id == checkId, cancellationToken)
            ?? throw new InvalidOperationException("Check not found.");

        check.Status = status;
        check.Answer = answer;
        check.Notes = notes;
        check.Source = source;
        check.UpdatedAt = DateTime.UtcNow;

        // Keep the reusable candidate details on the lead up to date.
        var lead = check.Qualification?.Lead;
        if (lead is not null && !string.IsNullOrWhiteSpace(answer))
        {
            switch (check.Criterion)
            {
                case InterviewReadyCriterion.Pay:
                    lead.PayExpectation = answer;
                    break;
                case InterviewReadyCriterion.WorkAuthorisation:
                    lead.WorkAuthorisation = answer;
                    break;
                case InterviewReadyCriterion.Licences:
                    lead.LicencesHeld = answer;
                    break;
            }

            lead.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return check;
    }

    public async Task<QualificationCheck> ConfirmCheckAsync(int checkId, int userId, CancellationToken cancellationToken = default)
    {
        var check = await _dbContext.QualificationChecks
            .IgnoreQueryFilters()
            .Include(c => c.Qualification)
            .FirstOrDefaultAsync(c => c.Id == checkId, cancellationToken)
            ?? throw new InvalidOperationException("Check not found.");

        check.Source = CheckSource.Human;
        check.ConfirmedByUserId = userId;
        check.ConfirmedAt = DateTime.UtcNow;
        check.UpdatedAt = DateTime.UtcNow;
        if (check.Status == InterviewReadyStatus.Unknown)
        {
            check.Status = InterviewReadyStatus.Met;
        }

        if (check.Qualification is not null)
        {
            check.Qualification.ConfirmedByUserId = userId;
            check.Qualification.ConfirmedAt = DateTime.UtcNow;
            check.Qualification.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return check;
    }

    public async Task<CandidateQualification> UpdateHeaderAsync(
        int id,
        string? payExpectation,
        string? availability,
        string? noticePeriod,
        CancellationToken cancellationToken = default)
    {
        var qualification = await _dbContext.CandidateQualifications
            .IgnoreQueryFilters()
            .Include(q => q.Checks)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Qualification not found.");

        qualification.PayExpectation = payExpectation;
        qualification.Availability = availability;
        qualification.NoticePeriod = noticePeriod;
        qualification.UpdatedAt = DateTime.UtcNow;

        var lead = await _dbContext.Leads
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == qualification.LeadId, cancellationToken);
        if (lead is not null)
        {
            lead.PayExpectation = payExpectation;
            lead.Availability = availability;
            lead.NoticePeriod = noticePeriod;
            lead.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return qualification;
    }

    public async Task<string> BuildHandoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var qualification = await _dbContext.CandidateQualifications
            .IgnoreQueryFilters()
            .Include(q => q.Lead)
            .Include(q => q.Checks)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Qualification not found.");

        var job = qualification.JobId is int jobId
            ? await _dbContext.Jobs.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken)
            : null;
        var client = job?.ClientId is int clientId
            ? await _dbContext.Clients.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken)
            : null;

        var candidate = $"{qualification.Lead.FirstName} {qualification.Lead.LastName}".Trim();
        var sb = new StringBuilder();
        sb.AppendLine("<h3>Interview-ready handover</h3>");
        sb.AppendLine($"<p><strong>Candidate:</strong> {Encode(candidate)} ({Encode(qualification.Lead.Email)})</p>");
        sb.AppendLine($"<p><strong>Role:</strong> {Encode(job?.Title ?? "Not linked")}" +
                      (client is null ? "" : $" · <strong>Client:</strong> {Encode(client.Name)}") + "</p>");
        sb.AppendLine($"<p><strong>Pay expectation:</strong> {Encode(qualification.PayExpectation ?? "—")}<br/>" +
                      $"<strong>Availability:</strong> {Encode(qualification.Availability ?? "—")}<br/>" +
                      $"<strong>Notice period:</strong> {Encode(qualification.NoticePeriod ?? "—")}</p>");

        sb.AppendLine("<ul>");
        foreach (var check in qualification.Checks.OrderBy(c => c.Criterion))
        {
            sb.AppendLine($"<li><strong>{Encode(CriterionLabel(check.Criterion))}:</strong> {Encode(StatusLabel(check.Status))}" +
                          (string.IsNullOrWhiteSpace(check.Answer) ? "" : $" — {Encode(check.Answer)}") +
                          (string.IsNullOrWhiteSpace(check.Notes) ? "" : $" <em>({Encode(check.Notes)})</em>") +
                          "</li>");
        }
        sb.AppendLine("</ul>");

        return sb.ToString();
    }

    public async Task<CandidateQualification> MarkHandoverSentAsync(int id, string? to, CancellationToken cancellationToken = default)
    {
        var qualification = await _dbContext.CandidateQualifications
            .IgnoreQueryFilters()
            .Include(q => q.Checks)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Qualification not found.");

        qualification.HandoverSentAt = DateTime.UtcNow;
        qualification.HandoverSentTo = to;
        qualification.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return qualification;
    }

    // Populate the candidate's talent-pool tags from the job, unless already set.
    private static bool ApplyJobTags(Lead lead, Job? job)
    {
        if (job is null)
        {
            return false;
        }

        var changed = false;
        if (string.IsNullOrWhiteSpace(lead.IndustryTag) && !string.IsNullOrWhiteSpace(job.Industry))
        {
            lead.IndustryTag = job.Industry;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(lead.RoleFamilyTag) && !string.IsNullOrWhiteSpace(job.RoleFamily))
        {
            lead.RoleFamilyTag = job.RoleFamily;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(lead.JobTitleTag) && !string.IsNullOrWhiteSpace(job.Title))
        {
            lead.JobTitleTag = job.Title;
            changed = true;
        }

        if (changed)
        {
            lead.UpdatedAt = DateTime.UtcNow;
        }

        return changed;
    }

    private static QualificationCheck NewCheck(int userId, InterviewReadyCheckTemplate item) => new()
    {
        UserId = userId,
        Criterion = item.Criterion,
        Prompt = item.Prompt,
        Expected = item.Expected,
        Status = InterviewReadyStatus.Unknown,
        Source = CheckSource.Agent,
        UpdatedAt = DateTime.UtcNow
    };

    private async Task<int?> ResolveJobIdAsync(int leadId, int? jobId, int? campaignId, CancellationToken cancellationToken)
    {
        if (jobId is int explicitJob)
        {
            return explicitJob;
        }

        if (campaignId is int campaign)
        {
            var campaignJob = await _dbContext.Campaigns
                .IgnoreQueryFilters()
                .Where(c => c.Id == campaign)
                .Select(c => c.JobId)
                .FirstOrDefaultAsync(cancellationToken);
            if (campaignJob is int job)
            {
                return job;
            }
        }

        var interviewJob = await _dbContext.Interviews
            .IgnoreQueryFilters()
            .Where(i => i.LeadId == leadId && i.JobId != null && i.Status != InterviewStatus.Cancelled)
            .OrderByDescending(i => i.StartAt)
            .Select(i => i.JobId)
            .FirstOrDefaultAsync(cancellationToken);
        if (interviewJob is int fromInterview)
        {
            return fromInterview;
        }

        var latestCampaign = await _dbContext.OutreachMessages
            .IgnoreQueryFilters()
            .Where(m => m.LeadId == leadId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (int?)m.CampaignId)
            .FirstOrDefaultAsync(cancellationToken);
        if (latestCampaign is int campaignId2)
        {
            return await _dbContext.Campaigns
                .IgnoreQueryFilters()
                .Where(c => c.Id == campaignId2)
                .Select(c => c.JobId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    public static string CriterionLabel(InterviewReadyCriterion criterion) => criterion switch
    {
        InterviewReadyCriterion.RoleRelevance => "Role relevance",
        InterviewReadyCriterion.RoleUnderstanding => "Role understanding",
        InterviewReadyCriterion.WorkHours => "Work hours",
        InterviewReadyCriterion.Location => "Location",
        InterviewReadyCriterion.Pay => "Pay",
        InterviewReadyCriterion.WorkAuthorisation => "Work authorisation",
        InterviewReadyCriterion.BackgroundChecks => "Background checks",
        InterviewReadyCriterion.Licences => "Licences and certifications",
        InterviewReadyCriterion.Interest => "Interest and intent",
        InterviewReadyCriterion.HumanConfirmation => "Human confirmation",
        InterviewReadyCriterion.InterviewArranged => "Interview arranged",
        _ => criterion.ToString()
    };

    public static string StatusLabel(InterviewReadyStatus status) => status switch
    {
        InterviewReadyStatus.Met => "Met",
        InterviewReadyStatus.NotMet => "Not met",
        InterviewReadyStatus.NotApplicable => "Not applicable",
        _ => "Unknown"
    };

    private static string Encode(string? value) => string.IsNullOrEmpty(value)
        ? string.Empty
        : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
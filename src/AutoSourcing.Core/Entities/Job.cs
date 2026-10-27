using AutoSourcing.Core.Abstractions;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class Job : IOwnedEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ClientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Industry { get; set; }

    // Role classification, used for grouping/reporting and talent-pool tagging.
    public string? RoleFamily { get; set; }
    public string? AlternativeTitles { get; set; }
    public string? AdjacentIndustries { get; set; }
    public int Vacancies { get; set; } = 1;

    // Interview-ready brief (the agent asks the candidate these).
    public string? ShiftPattern { get; set; }
    public string? WorkingHours { get; set; }
    public bool OnCallRequired { get; set; }
    public bool WorkAuthorisationRequired { get; set; } = true;
    public bool SponsorshipAvailable { get; set; }
    public string? BackgroundChecks { get; set; }
    public string? RequiredLicences { get; set; }
    public string? ExclusionCriteria { get; set; }
    public string? TravelRequirement { get; set; }
    public bool RelocationRequired { get; set; }
    public string? RoleSummary { get; set; }

    public JobType Type { get; set; } = JobType.FullTime;
    public Flexibility Flexibility { get; set; } = Flexibility.OnSite;
    public SalaryType SalaryType { get; set; } = SalaryType.NotDisclosed;
    public decimal? SalaryFrom { get; set; }
    public decimal? SalaryTo { get; set; }
    public string? SalaryNotes { get; set; }
    public string? PayCurrency { get; set; }
    public PayPeriod PayPeriod { get; set; } = PayPeriod.Year;
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? HiringManager { get; set; }
    public string? Department { get; set; }
    public string? AdvertUrl { get; set; }
    public string? AdvertCopy { get; set; }
    public string? MustHaves { get; set; }
    public string? NiceToHaves { get; set; }
    public string? Education { get; set; }
    public string? Skills { get; set; }
    public string? AttractiveReasons { get; set; }
    public string? ScreeningDetails { get; set; }
    public bool AllowClientNameInMessages { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

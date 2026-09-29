using AutoSourcing.Core.Enums;

namespace AutoSourcing.Core.Entities;

public class Job
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Industry { get; set; }
    public JobType Type { get; set; } = JobType.FullTime;
    public Flexibility Flexibility { get; set; } = Flexibility.OnSite;
    public SalaryType SalaryType { get; set; } = SalaryType.NotDisclosed;
    public decimal? SalaryFrom { get; set; }
    public decimal? SalaryTo { get; set; }
    public string? SalaryNotes { get; set; }
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
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

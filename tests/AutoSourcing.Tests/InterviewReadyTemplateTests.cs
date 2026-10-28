using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;
using AutoSourcing.Services.Qualification;
using Xunit;

namespace AutoSourcing.Tests;

public class InterviewReadyTemplateTests
{
    [Fact]
    public void Build_ReturnsTheElevenFixedCriteriaInOrder()
    {
        var checks = InterviewReadyTemplate.Build(new Job());

        Assert.Equal(11, checks.Count);
        Assert.Equal(InterviewReadyCriterion.RoleRelevance, checks[0].Criterion);
        Assert.Equal(InterviewReadyCriterion.HumanConfirmation, checks[9].Criterion);
        Assert.Equal(InterviewReadyCriterion.InterviewArranged, checks[10].Criterion);
        Assert.Equal(11, checks.Select(c => c.Criterion).Distinct().Count());
    }

    [Fact]
    public void Build_FillsTheExpectedDetailFromTheJobBrief()
    {
        var job = new Job
        {
            Location = "Eemshaven, Netherlands",
            ShiftPattern = "12-hour rotating",
            WorkingHours = "40 hours per week",
            PayCurrency = "EUR",
            SalaryFrom = 50000,
            SalaryTo = 60000,
            PayPeriod = PayPeriod.Year,
            RequiredLicences = "HV switching ticket",
            BackgroundChecks = "Drug screening",
        };

        var checks = InterviewReadyTemplate.Build(job);

        Assert.Contains("12-hour rotating", checks.Single(c => c.Criterion == InterviewReadyCriterion.WorkHours).Expected);
        Assert.Contains("Eemshaven", checks.Single(c => c.Criterion == InterviewReadyCriterion.Location).Expected);
        Assert.Contains("EUR", checks.Single(c => c.Criterion == InterviewReadyCriterion.Pay).Expected);
        Assert.Equal("HV switching ticket", checks.Single(c => c.Criterion == InterviewReadyCriterion.Licences).Expected);
        Assert.Equal("Drug screening", checks.Single(c => c.Criterion == InterviewReadyCriterion.BackgroundChecks).Expected);
    }

    [Fact]
    public void Build_HandlesNoJob()
    {
        var checks = InterviewReadyTemplate.Build(null);

        Assert.Equal(11, checks.Count);
        Assert.All(checks, check => Assert.False(string.IsNullOrWhiteSpace(check.Prompt)));
    }
}
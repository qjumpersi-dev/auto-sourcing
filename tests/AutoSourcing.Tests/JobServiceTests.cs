using AutoSourcing.Core.Entities;
using AutoSourcing.Services.Jobs;
using Xunit;

namespace AutoSourcing.Tests;

public class JobServiceTests
{
    private readonly JobService _service = new();

    [Fact]
    public void TwoPartLocation_TreatsSecondPartAsCountry()
    {
        var job = new Job { Title = "Data Center Technician", Location = "Eemshaven, Netherlands" };

        var request = _service.BuildSearchSpecFromJob(job);

        Assert.Equal(new[] { "Eemshaven" }, request.Cities);
        Assert.Equal(new[] { "Netherlands" }, request.Countries);
        Assert.Empty(request.States);
    }

    [Fact]
    public void ThreePartLocation_SplitsCityStateCountry()
    {
        var job = new Job { Title = "Engineer", Location = "Dallas, TX, USA" };

        var request = _service.BuildSearchSpecFromJob(job);

        Assert.Equal(new[] { "Dallas" }, request.Cities);
        Assert.Equal(new[] { "TX" }, request.States);
        Assert.Equal(new[] { "USA" }, request.Countries);
    }

    [Fact]
    public void TwoLetterSecondPart_IsTreatedAsState()
    {
        var job = new Job { Title = "Engineer", Location = "Austin, TX" };

        var request = _service.BuildSearchSpecFromJob(job);

        Assert.Equal(new[] { "Austin" }, request.Cities);
        Assert.Equal(new[] { "TX" }, request.States);
        Assert.Empty(request.Countries);
    }

    [Fact]
    public void SinglePartLocation_IsTreatedAsCity()
    {
        var job = new Job { Title = "Engineer", Location = "Auckland" };

        var request = _service.BuildSearchSpecFromJob(job);

        Assert.Equal(new[] { "Auckland" }, request.Cities);
        Assert.Empty(request.States);
        Assert.Empty(request.Countries);
    }
}
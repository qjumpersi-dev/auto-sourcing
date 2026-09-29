using AutoSourcing.Core.Entities;
using AutoSourcing.Services.Rhetorik;

namespace AutoSourcing.Services.Jobs;

public interface IJobService
{
    ProfileSearchRequest BuildSearchSpecFromJob(Job job);
}

public class JobService : IJobService
{
    public ProfileSearchRequest BuildSearchSpecFromJob(Job job)
    {
        var request = new ProfileSearchRequest();

        if (!string.IsNullOrWhiteSpace(job.Title))
        {
            request.JobTitles = new List<string> { job.Title };
        }

        if (!string.IsNullOrWhiteSpace(job.Location))
        {
            var parts = job.Location.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (parts.Count == 1)
            {
                request.Cities = new List<string> { parts[0] };
            }
            else if (parts.Count >= 2)
            {
                request.Cities = new List<string> { parts[0] };
                request.States = new List<string> { parts[1] };
            }
            if (parts.Count >= 3)
            {
                request.Countries = new List<string> { parts[2] };
            }
        }

        var keywords = new List<string>();
        if (!string.IsNullOrWhiteSpace(job.Industry))
        {
            keywords.Add(job.Industry);
        }
        if (!string.IsNullOrWhiteSpace(job.Department))
        {
            keywords.Add(job.Department);
        }
        if (keywords.Count > 0)
        {
            request.Keywords = keywords;
        }

        var expertises = new List<string>();
        if (!string.IsNullOrWhiteSpace(job.Skills))
        {
            expertises.AddRange(job.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s)));
        }
        if (!string.IsNullOrWhiteSpace(job.MustHaves))
        {
            expertises.AddRange(job.MustHaves.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s)));
        }
        if (expertises.Count > 0)
        {
            request.Expertises = expertises.Distinct().ToList();
            request.ExpertiseMode = "must_have_any";
        }

        return request;
    }
}

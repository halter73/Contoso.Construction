using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MinimalApis.Extensions.Results;
using Contoso.Construction.Shared;

namespace Contoso.Construction.Server.Services;

public class JobsService
{
    private readonly JobSiteDb _database;
    public JobsService(JobSiteDb database)
    {
        _database = database;
    }

    public async Task<List<Job>> GetAllJobs()
    {
        return await _database.Jobs.ToListAsync();
    }

    public async Task<IResult> GetJobById(int id)
    {
        var job = await _database.Jobs.FirstOrDefaultAsync(jobs => jobs.Id == id);
        return job is null ? Results.Extensions.NotFound() : Results.Extensions.Ok(job);
    }

    public async Task<IResult> CreateJob(Job job)
    {
        _database.Jobs.Add(job);
        await _database.SaveChangesAsync();
        return Results.Extensions.Created(
            $"/jobs/{job.Id}", job);
    }

    public IEnumerable<Job> GetJobsByQuery(string query)
    {
        var jobs = _database.Jobs.Where(x => x.Name.Contains(query));
        return jobs.Any() ? jobs.AsEnumerable() : Array.Empty<Job>();
    }
}
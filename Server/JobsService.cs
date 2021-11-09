using Contoso.Construction.Shared;
using Microsoft.EntityFrameworkCore;
using MinimalApis.Extensions.Results;
using System.Linq.Expressions;

namespace Contoso.Construction.Server;

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
        var job = await _database.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        return job is null ? Results.Extensions.NotFound() : Results.Extensions.Ok(job);
    }

    public async Task<IResult> CreateJob(Job job)
    {
        _database.Jobs.Add(job);
        await _database.SaveChangesAsync();
        return Results.Extensions.Created(
            $"/jobs/{job.Id}", job);
    }

    public Task<List<Job>> GetJobsByName(string query) => GetJobsWhere(j => j.Name.Contains(query));

    public Task<List<Job>> GetJobsWhere(Expression<Func<Job, bool>> predicate) =>
        _database.Jobs.Where(predicate).ToListAsync();
}
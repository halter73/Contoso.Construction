using Microsoft.EntityFrameworkCore;

namespace Contoso.Construction.Shared;

public class JobSiteDb : DbContext
{
    public JobSiteDb() { }
    public JobSiteDb(
        DbContextOptions<JobSiteDb> options)
        : base(options) { }

    // Virtual to support mocking in unit tests
    public virtual DbSet<Job> Jobs
        => Set<Job>();
}
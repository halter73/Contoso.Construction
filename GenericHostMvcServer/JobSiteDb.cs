using Microsoft.EntityFrameworkCore;

namespace GenericHostMvcServer
{
    public class JobSiteDb : DbContext
    {
        public JobSiteDb(
            DbContextOptions<JobSiteDb> options)
            : base(options) { }

        public DbSet<Job> Jobs
            => Set<Job>();
    }
}

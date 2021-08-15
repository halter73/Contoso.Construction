using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GenericHostMvcServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly JobSiteDb _db;

        public JobsController(JobSiteDb db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IEnumerable<Job>> Get()
        {
            return await _db.Jobs.ToListAsync();
        }

        [HttpPost]
        public async Task Post(Job job)
        {
            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();

            HttpContext.Response.StatusCode = 201;
            HttpContext.Response.Headers["Location"] = $"/jobs/{job.Id}";
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return await _db.Jobs.FirstOrDefaultAsync(jobs => jobs.Id == id)
                is Job job
                    ? new OkObjectResult(job)
                    : new NotFoundResult();
        }

        [HttpGet("search/name/{query}")]
        public async Task<IActionResult> GetFromQuery(string query)
        {
            var jobs = await _db.Jobs
                .Where(x => x.Name.Contains(query))
                .ToListAsync();

            return jobs.Any()
                    ? new OkObjectResult(jobs)
                    : new NotFoundObjectResult(new Job[] { });
        }
    }
}

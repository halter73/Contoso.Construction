using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Contoso.Construction.Server;
using Contoso.Construction.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add the Azure Key Vault configuration provider
if (!string.IsNullOrEmpty(builder.Configuration["VaultUri"]))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(builder.Configuration["VaultUri"]),
        new DefaultAzureCredential());
}

// // Add the Entity Framework Core DBContext
builder.Services.AddDbContext<JobSiteDb>(options =>
{
    options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("AzureSqlConnectionString"));
});

// Enable the API explorer
builder.Services.AddEndpointsApiExplorer();

// Add OpenAPI services to the container.
builder.Services.AddSwaggerGen();

// Enable Blazor WASM hosting
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<JobsService>();

// Build the app
var app = builder.Build();

// Configure for development 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Make sure the SQL DB schema has been created
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<JobSiteDb>();
        db.Database.EnsureCreated();
    }
}

// Enables GET of all jobs
app.MapGet("/jobs",
    async (JobsService jobs) =>
    {
        return await jobs.GetAllJobs();
    })
    .Produces<List<Job>>(StatusCodes.Status200OK)
    .WithName("GetAllJobs")
    .WithTags("Getters");

// Enables GET of all jobs as XML
app.MapGet("/jobsxml",
    async (JobsService jobs) =>
    {
        return Results.Extensions.Xml(await jobs.GetAllJobs());
    })
    .Produces<List<Job>>(StatusCodes.Status200OK, "application/xml")
    .WithName("GetAllJobsAsXml")
    .WithTags("Getters");

// Enables GET of a specific job
app.MapGet("/jobs/{id}",
    async (int id, JobsService jobs) =>
    {
        return await jobs.GetJobById(id);
    })
    .Produces<Job>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetJob")
    .WithTags("Getters");

// Enables creation of a new job 
app.MapPost("/jobs/", 
    async (Job job, JobsService jobs) =>
    {
        return await jobs.CreateJob(job);
    })
    .Accepts<Job>("application/json")
    .Produces<Job>(StatusCodes.Status201Created)
    .WithName("CreateJob")
    .WithTags("Creators")
    .RequireAuthorization();

// Enables searching for a job
app.MapGet("/jobs/search/name/{query}",
    async (string query, JobsService jobs) =>
    {
        var result = await jobs.GetJobsByName(query);
        return !result.Any() ? Results.NotFound() : Results.Ok(jobs);
    })
    .Produces<List<Job>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("SearchJobsByName")
    .WithTags("Getters");

// Search by lat,lon
app.MapGet("/jobs/search/location/{coordinate}",
    (Coordinate coordinate, JobsService jobs) =>
        jobs.GetJobsWhere(job =>
           job.Latitude > coordinate.Latitude - 1 &&
           job.Latitude < coordinate.Latitude + 1 &&
           job.Longitude > coordinate.Longitude - 1 &&
           job.Longitude < coordinate.Longitude + 1
        )
    )
    .WithName("SearchJobsByLocation")
    .WithTags("Getters");

// Register middleware for hosting Blazor apps.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapFallbackToFile("index.html");

// Start the host and run the app
app.Run();

using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add the Azure Key Vault configuration provider
if(!string.IsNullOrEmpty(builder.Configuration["VaultUri"]))
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
    async (JobSiteDb db) =>
        await db.Jobs.ToListAsync()
    )
    .Produces<List<Job>>(StatusCodes.Status200OK)
    .WithName("GetAllJobs")
    .WithTags("Getters");

// Enables GET of a specific job
app.MapGet("/jobs/{id}",
    async (int id, JobSiteDb db) =>
        await db.Jobs.FirstOrDefaultAsync(jobs => jobs.Id == id)
            is Job job
                ? Results.Ok(job)
                : Results.NotFound()
    )
    .Produces<Job>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetJob")
    .WithTags("Getters");

// Enables creation of a new job 
app.MapPost("/jobs/", 
    async (Job job, JobSiteDb db) =>
    {
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/jobs/{job.Id}", job); 
    })
    .Accepts<Job>("application/json")
    .Produces<Job>(StatusCodes.Status201Created)
    .WithName("CreateJob")
    .WithTags("Creators");

// Enables searching for a job
app.MapGet("/jobs/search/{query}",
    (string query, JobSiteDb db) =>
        db.Jobs
            .Where(x => x.Name.Contains(query))
            is IEnumerable<Job> jobs
                ? Results.Ok(jobs)
                : Results.NotFound(Array.Empty<Job>())
    )
    .Produces<List<Job>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("SearchJobs")
    .WithTags("Getters");

// Register middlewares for hosting Blazor apps.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapFallbackToFile("index.html");

// Start the host and run the app
app.Run();

class JobSiteDb : DbContext
{
    public JobSiteDb(
        DbContextOptions<JobSiteDb> options)
        : base(options) { }

    public DbSet<Job> Jobs
        => Set<Job>();
}
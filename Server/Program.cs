using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using System.ComponentModel.DataAnnotations.Schema;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------
// Note: This is commented out to enable "clone-
// to-build success, as until you've created the
// key vault resource and put the Vault URI into
// Properties/launchSettings.json, this code will
// result with a compilation error. Run the setup
// scripts in the "setup" folder in the GitHub 
// repo, and then you can paste the Vault URI. 
// ----------------------------------------------

// Add the Azure Key Vault configuration provider
if(!string.IsNullOrEmpty(builder.Configuration["VaultUri"]))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(builder.Configuration["VaultUri"]),
        new DefaultAzureCredential());
}

// Add the Entity Framework Core DBContext
builder.Services.AddDbContext<JobSiteDb>(options =>
{
    options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("AzureSqlConnectionString"));
});

// Add Azure Storage services to the app
builder.Services.AddAzureClients(options =>
{
    options.AddBlobServiceClient(
        builder.Configuration["AzureStorageConnectionString"]);
});

// Enable the API explorer
builder.Services.AddEndpointsApiExplorer();

// Enable Blazor WASM hosting
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// The OpenAPI description name
var openApiDesc = "Contoso.JobSiteAppApi";

// Add OpenAPI services to the container.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(openApiDesc, new() 
    { 
        Title = "Job Site Survey App API", 
        Version = "2021-11-01" 
    });
});

// Build the app
var app = builder.Build();

// Configure for development 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.SerializeAsV2 = true;
        c.RouteTemplate = "/{documentName}.json";
    });

    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint(
            $"/{openApiDesc}.json", openApiDesc)
        );

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
    .WithName("GetAllJobs");

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
    .WithName("GetJob");

// Enables creation of a new job 
app.MapPost("/jobs/", 
    async (Job job, JobSiteDb db) =>
    {
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/jobs/{job.Id}", job); 
    })
    .Produces<Job>(StatusCodes.Status201Created)
    .WithName("CreateJob");

// Enables searching for a job
app.MapGet("/jobs/search/{query}",
    (string query, JobSiteDb db) =>
        db.Jobs
            .Where(x => x.Name.Contains(query))
            is IEnumerable<Job> jobs
                ? Results.Ok(jobs)
                : Results.NotFound(new Job[] { })
    )
    .Produces<List<Job>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("SearchJobs");

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapRazorPages();
app.MapFallbackToFile("index.html");

// Start the host and run the app
app.Run();

public class Job
{
    [DatabaseGenerated(
        DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; }
        = string.Empty;
}

class JobSiteDb : DbContext
{
    public JobSiteDb(
        DbContextOptions<JobSiteDb> options)
        : base(options) { }

    public DbSet<Job> Jobs
        => Set<Job>();
}
using Azure.Identity;
using Azure.Storage.Blobs;
using Contoso.Construction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using System.ComponentModel.DataAnnotations.Schema;

var builder = WebApplication.CreateBuilder(args);

// Add the Azure Key Vault configuration provider
builder.Configuration.AddAzureKeyVault(
        new Uri(Environment.GetEnvironmentVariable("VaultUri")),
        new DefaultAzureCredential());

// Add the Entity Framework Core DBContext
builder.Services.AddDbContext<JobSiteDb>(_ =>
{
    _.UseSqlServer(
        builder.Configuration
            .GetConnectionString(
                "AzureSqlConnectionString"));
});

// Add Azure Storage services to the app
builder.Services.AddAzureClients(_ =>
{
    _.AddBlobServiceClient(
        builder.Configuration
            ["AzureStorageConnectionString"]
        );
});

// Enable the API explorer
builder.Services.AddEndpointsApiExplorer();

// The OpenAPI description name
var openApiDesc = "Contoso.JobSiteAppApi";

// Add OpenAPI services to the container.
builder.Services.AddSwaggerGen(_ =>
{
    _.OperationFilter<ImageExtensionFilter>();
    _.SwaggerDoc(openApiDesc, new() 
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
        await db.Jobs
                .Include("Photos")
                    .FirstOrDefaultAsync(_ =>
                        _.Id == id)
            is Job job
                ? Results.Ok(job)
                : Results.NotFound()
    )
    .Produces<Job>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetJob");

// Enables creation of a new job 
app.MapPost("/jobs/", 
    async (Job job, 
        JobSiteDb db) =>
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
            .Include("Photos")
            .Where(x => x.Name.Contains(query))
            is IEnumerable<Job> jobs
                ? Results.Ok(jobs)
                : Results.NotFound(new Job[] { })
    )
    .Produces<List<Job>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("SearchJobs");

// Upload a site photo
app.MapPost(
    "/jobs/{jobId}/photos/{lat}/{lng}/{heading}", 
    async (HttpRequest req,
        int jobId,
        double lat,
        double lng,
        int heading,
        BlobServiceClient blobServiceClient,
        JobSiteDb db) =>
    {
        if (!req.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await req.ReadFormAsync();
        var file = form.Files["file"];

        if (file is null) 
            return Results.BadRequest();

        using var upStream = 
           file.OpenReadStream();

        var blobClient = blobServiceClient
               .GetBlobContainerClient("uploads")
                   .GetBlobClient(file.FileName);

        await blobClient.UploadAsync(upStream);

        db.JobSitePhotos.Add(new JobSitePhoto
        {
            JobId = jobId,
            Latitude = lat,
            Longitude = lng,
            Heading = heading,
            PhotoUploadUrl =
                blobClient.Uri.AbsoluteUri
        });

        await db.SaveChangesAsync();

        var job = await db.Jobs
                    .Include("Photos")
                    .FirstOrDefaultAsync(x =>
                        x.Id == jobId);

        return Results.Created(
            $"/jobs/{jobId}", job);
    })
    .Produces<Job>(StatusCodes.Status200OK,
        "application/json")
    .WithName(
        ImageExtensionFilter
            .UPLOAD_SITE_PHOTO_OPERATION_ID);

// Start the host and run the app
app.Run();

// ----------------------------------------------
// Site Job Data Code
// ----------------------------------------------
public class JobSitePhoto
{
    [DatabaseGenerated(
        DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int Heading { get; set; }
    public int JobId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string PhotoUploadUrl { get; set; }
        = string.Empty;
}

public class Job
{
    [DatabaseGenerated(
        DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; }
        = string.Empty;
    public List<JobSitePhoto> Photos 
        { get; set; } = new List<JobSitePhoto>();
}

class JobSiteDb : DbContext
{
    public JobSiteDb(
        DbContextOptions<JobSiteDb> options)
        : base(options) { }

    public DbSet<Job> Jobs
        => Set<Job>();

    public DbSet<JobSitePhoto> JobSitePhotos
        => Set<JobSitePhoto>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>()
                .HasMany(s => s.Photos);

        base.OnModelCreating(modelBuilder);
    }
}
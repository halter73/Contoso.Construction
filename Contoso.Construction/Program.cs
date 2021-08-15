using Azure.Identity;
using Azure.Storage.Blobs;
using Contoso.Construction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using System.ComponentModel.DataAnnotations.Schema;

// ----------------------------------------------
// Site Job API Code
// ----------------------------------------------
var builder = WebApplication.CreateBuilder(args);

// Add the Azure Key Vault configuration provider
builder.Host.ConfigureAppConfiguration(
    (context, config) =>
    {
        var uri = Environment
            .GetEnvironmentVariable(
                "AzureKeyVaultUri");

        config.AddAzureKeyVault(
            new Uri(uri),
            new DefaultAzureCredential());
    });

// Enable the API explorer
builder.Services.AddEndpointsApiExplorer();

// The OpenAPI description name
var openApiDesc = "Contoso.JobSiteAppApi";

// Add OpenAPI services to the container.
builder.Services.AddSwaggerGen(_ =>
{
    _.OperationFilter<ImageParameterExtensionFilter>();
    _.SwaggerDoc(openApiDesc, new() 
    { 
        Title = "Contoso Construction Job Site", 
        Version = "2021-11-01" 
    });
});

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
}

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
    .WithName("CreateJob");

// Enables GET of all jobs
app.MapGet("/jobs",
    async (JobSiteDb db) => 
        await db.Jobs.ToListAsync()
    )
    .WithName("GetAllJobs");

// Enables GET of a specific job
app.MapGet("/jobs/{id}",
    async (int id, JobSiteDb db) =>
        await db.Jobs
                .Include("Photos")
                .FirstOrDefaultAsync(x => 
                    x.Id == id)
            is Job job
                ? Results.Ok(job)
                : Results.NotFound()
    )
    .WithName("GetJob");

// Upload a site photo
app.MapPost("/jobs/{jobId}/photos/upload", 
    async (HttpRequest req,
        int jobId,
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

        var name = file.FileName;
        using var upStream = 
           file.OpenReadStream();
        var blobClient = blobServiceClient
               .GetBlobContainerClient("uploads")
                   .GetBlobClient(name);

        await blobClient.UploadAsync(upStream);

        return Results.Created(
            blobClient.Uri.AbsoluteUri,
            blobClient.Uri.AbsoluteUri);
    })
    .WithName(
        ImageParameterExtensionFilter
            .UPLOAD_SITE_PHOTO_OPERATION_ID);

// Save the metadata for the site
app.MapPost("/jobs/{jobId}/photos", 
    async (int jobId,
        JobSitePhoto photo,
        BlobServiceClient blobServiceClient,
        JobSiteDb db) =>
    {
        db.JobSitePhotos.Add(photo);
        await db.SaveChangesAsync();

        var job = await db.Jobs
                    .Include("Photos")
                    .FirstOrDefaultAsync(x =>
                        x.Id == jobId);

        return Results.Created(
            $"/jobs/{jobId}", job);
    })
    .WithName("CreateJobSitePhoto");

app.Run();

// ----------------------------------------------
// Site Job Data Code
// ----------------------------------------------
public class JobSitePhoto
{
    [DatabaseGenerated(
        DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string PhotoUploadUrl { get; set; } 
        = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Heading { get; set; }
    public int JobId { get; set; }
}

public class Job
{
    [DatabaseGenerated(
        DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
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
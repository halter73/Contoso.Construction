using Contoso.Construction;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
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

// Add application services to the container.
builder.Services.AddSwaggerGen(_ =>
{
    _.OperationFilter<AddFileParamTypes>();
    _.SwaggerDoc(openApiDesc, new() 
    { 
        Title = "Contoso Construction Job Site", 
        Version = "2021-11-01" 
    });
});

// Add the Entity Framework Core DBContext
builder.Services.AddDbContext<JobSiteDb>(_ =>
{
    _.UseSqlServer(builder.Configuration.GetConnectionString("AzureSqlConnectionString"));
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
    });

// Enables GET of all jobs
app.MapGet("/jobs",
    async (JobSiteDb db) => 
        await db.Jobs.ToListAsync()
        );

// Enables GET of a specific job
app.MapGet("/jobs/{id}",
    async (int id, JobSiteDb db) =>
        await db.Jobs.FindAsync(id)
            is Job job
                ? Results.Ok(job)
                : Results.NotFound()
                );

// Upload a site photo
app.MapPost("/upload", async (HttpRequest req) =>
{
    if (!req.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await req.ReadFormAsync();
    var file = form.Files["file"];

    if (file is null)
    {
        return Results.BadRequest();
    }

    var uploads = file.FileName;
    using var fileStream = 
        File.OpenWrite(uploads);
    using var uploadStream = 
        file.OpenReadStream();
    await uploadStream.CopyToAsync(fileStream);

    return Results.NoContent();
})
.WithName("UploadImage");

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
}
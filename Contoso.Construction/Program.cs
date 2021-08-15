using Contoso.Construction;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using Azure.Identity;

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
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AddFileParamTypes>();
    c.SwaggerDoc(openApiDesc, new() 
    { 
        Title = "Contoso Construction Job Site", 
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
}

// Enables creation of a new job 
app.MapPost("/jobs/", 
    async (JobSitePhoto jobSitePhoto, 
        JobSiteDb db) =>
    {
        db.JobSitePhotos.Add(jobSitePhoto);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/jobs/{jobSitePhoto.Id}", 
                jobSitePhoto); ;
    });

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
    public int Id { get; set; }
    public string PhotoUploadUrl { get; set; } 
        = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Heading { get; set; }
}

class JobSiteDb : DbContext
{
    public JobSiteDb(
        DbContextOptions<JobSiteDb> options)
        : base(options) { }

    public DbSet<JobSitePhoto> JobSitePhotos
        => Set<JobSitePhoto>();
}
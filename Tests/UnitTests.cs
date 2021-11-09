using Contoso.Construction.Server.Services;
using Contoso.Construction.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MinimalApis.Extensions.Results;
using Moq;
using Xunit;

public class UnitTests
{
    [Fact]
    public void GetJobsByQueryReturnsEmptyForInvalidQuery()
    {
        // Arrange
        var mockContext = CreateMockDbContext();
        var service = new JobsService(mockContext.Object);

        // Act
        var jobs = service.GetJobsByQuery("foo");
        
        // Assert
        Assert.Empty(jobs);
    }

    [Fact]
    public async Task CreateJobReturnsCorrectStatusCodeAndResponse()
    {
        // Arrange
        var jobToCreate = new Job { Latitude = -14.56198, Longitude = -54.52374, Name = "Job 4"};
        var mockContext = CreateMockDbContext();
        var service = new JobsService(mockContext.Object);

        // Act
        var result = await service.CreateJob(jobToCreate);

        // Assert
        var typedResult = Assert.IsType<Created<Job>>(result);
        Assert.Equal(typedResult.StatusCode, StatusCodes.Status201Created);
        Assert.Equal(typedResult.Value, jobToCreate);
    }

    private static Mock<JobSiteDb> CreateMockDbContext()
    {
        var data = new List<Job>
        {
            new Job { Latitude = -79.34766, Longitude = 88.15929, Name = "Job 1"},
            new Job { Latitude = 32.75046, Longitude = -16.35496, Name = "Job 2"},
            new Job { Latitude = 64.59402, Longitude = 72.52364, Name = "Job 3"}
        }.AsQueryable();
        var mockSet = new Mock<DbSet<Job>>();
        mockSet.As<IAsyncEnumerable<Job>>()
            .Setup(m => m.GetAsyncEnumerator(new CancellationToken()))
            .Returns(new TestDbAsyncEnumerator<Job>(data.GetEnumerator()));

        mockSet.As<IQueryable<Job>>()
            .Setup(m => m.Provider)
            .Returns(new TestDbAsyncQueryProvider<Job>(data.Provider));

        mockSet.As<IQueryable<Job>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<Job>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<Job>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());  

        var mockContext = new Mock<JobSiteDb>();
        mockContext.Setup(m => m.Jobs).Returns(mockSet.Object);

        return mockContext;
    }
}
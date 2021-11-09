using Contoso.Construction.Shared;
using System.Net;
using System.Net.Http.Json;
using Xunit;

public class IntegrationTests
{
    [Fact]
    public async Task POST_Jobs_ReturnsCreatedResponse()
    {
        // Arrange
        var app = new ApiApplication();

        var job = new Job()
        {
            Latitude = 53.30519,
            Longitude = -139.99564,
            Name = "Test Job"
        };
        var jobContent = JsonContent.Create(job);
        var expectedResponseBody = @"{""id"":1,""latitude"":53.30519,""longitude"":-139.99564,""name"":""Test Job""}";

        // Act
        var client = app.CreateClient();
        var response = await client.PostAsync("/jobs", jobContent);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(expectedResponseBody, responseBody);
    }
}
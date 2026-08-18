using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OutageTracker.Tests;

public class RateLimitingTests
{
    [Fact]
    public async Task ByRegion_WithinLimit_ReturnsOk()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        for (var requestNumber = 0; requestNumber < 10; requestNumber++)
        {
            var response = await client.GetAsync("/outages/by-region?region=East-01");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ByRegion_OverLimit_Returns429()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        for (var requestNumber = 0; requestNumber < 10; requestNumber++)
        {
            var response = await client.GetAsync("/outages/by-region?region=East-01");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var limitedResponse = await client.GetAsync("/outages/by-region?region=East-01");
        var outagesResponse = await client.GetAsync("/outages");
        var outageResponse = await client.GetAsync("/outages/11111111-1111-1111-1111-111111111111");

        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
        Assert.Equal("60", limitedResponse.Headers.GetValues("Retry-After").Single());
        Assert.Equal(HttpStatusCode.OK, outagesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, outageResponse.StatusCode);
    }
}
